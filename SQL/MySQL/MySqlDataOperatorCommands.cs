//Copyright 2019 Volodymyr Podshyvalov
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

using System.Reflection;
using UniformDataOperator.Sql.Attributes;
using UniformDataOperator.Sql.Attributes.Modifiers;

namespace UniformDataOperator.Sql.MySql
{
    /// <summary>
    /// Operator that provides possibility to operate data on MySQL data base server.
    /// </summary>
    public partial class MySqlDataOperator : ISqlOperator
    { 
        /// <summary>
        /// Trying to set schema to databases server in case if schema not exist.
        /// </summary>
        /// <param name="schemaName">Name of the schema that would be used\created.</param>
        /// <param name="error">Error faces during operation.</param>
        /// <returns></returns>
        public bool ActivateSchema(string schemaName, out string error)
        {
            // Check is SQL operator exist.
            if (Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
            }

            // Variable that would contain SQL comand.
            string command = "";

            // Creating schema if not exist.
            command += "CREATE SCHEMA IF NOT EXISTS `" + schemaName + "` DEFAULT CHARACTER SET utf8 ;\n";

            // Setting schema as target.
            command += "USE `" + schemaName + "` ;";

            // Tring to open connection to the server.
            if (!Active.OpenConnection(out error))
            {
                // Inform about fail.
                return false;
            }

            // Execute command
            Active.NewCommand(command).ExecuteNonQuery();

            // Close connection after finish.
            Active.CloseConnection();

            // Confirm success.
            return true;
        }

        /// <summary>
        /// Return generated SQL command relative to init time.
        /// </summary>
        /// <param name="member">Member that contains defined attributes that describes column definition.</param>
        /// <returns>SQL command relative to MySql server.</returns>
        public string ColumnDeclarationCommand(MemberInfo member)
        {
            #region VALIDATION
            string command = "";
            // Get column descriptor.
            if (!AttributesHandler.TryToGetAttribute<Column>(member, out Column columnDescriptor))
            {
                // Drop if not column.
                return command;
            }
            #endregion

            #region TITLE
            // Set column title.
            command += "`" + columnDescriptor.title + "` ";
            #endregion

            #region TYPE
            // Detect specified overriding.
            if (AttributesHandler.TryToGetAttribute<Attributes.MySqlDBTypeOverride>(member, out Attributes.MySqlDBTypeOverride mySqlDBType))
            {
                command += mySqlDBType.stringFormat ?? mySqlDBType.type.ToString();
            }
            else
            {
                // Detect target type converted from common via param.
                command += SqlOperatorHandler.Active.DbTypeToString(columnDescriptor.type);
            }
            #endregion

            #region ZERO FILL
            if (AttributesHandler.HasAttribute<IsZeroFill>(member))
            {
                command += " ZEROFILL";
            }
            #endregion

            #region BINARY
            if (AttributesHandler.HasAttribute<IsBinary>(member))
            {
                command += " BINARY";
            }
            #endregion

            #region UNASIGNED
            if (AttributesHandler.HasAttribute<IsUnsigned>(member))
            {
                command += " UNSIGNED";
            }
            #endregion

            #region Generated | Default
            if (AttributesHandler.TryToGetAttribute<Default>(member, out Default hasDefault) &&
                !string.IsNullOrEmpty(hasDefault.defExp))
            {
                // If generated
                if (hasDefault is IsGenerated isGenerated)
                {
                    command += " GENERATED ALWAYS AS(";
                    command += isGenerated.defExp + ") ";
                    command += (isGenerated.mode == IsGenerated.Mode.Stored ? "STORED" : "VIRTUAL");
                }
                // If  has default.
                else
                {
                    command += " DEFAULT " + hasDefault.defExp;
                }
            }
            #endregion

            #region NULL | NOT NULL
            // If not generated.
            if (hasDefault == null || !(hasDefault is IsGenerated))
            {
                if (AttributesHandler.HasAttribute<IsPrimaryKey>(member))
                {
                    command += " NOT NULL";
                }
                else
                {
                    // If has NotNull attribute.
                    if (AttributesHandler.HasAttribute<IsNotNull>(member))
                    {
                        command += " NOT NULL";
                    }
                    else
                    {
                        command += " NULL";
                    }
                }
            }
            #endregion

            #region AUTO INCREMENT
            // If has AutoIncrement attribute.
            if (AttributesHandler.HasAttribute<IsAutoIncrement>(member))
            {
                command += " AUTO_INCREMENT";
            }
            #endregion

            #region COMMENT
            // Add commentary.
            if (AttributesHandler.TryToGetAttribute<Commentary>(member, out Commentary commentary))
            {
                command += " COMMENT '" + commentary + "'";
            }
            #endregion

            return command;
        }

        #region Auto set to object data to database
        /// <summary>
        /// Generating set to table sql command from provided source data.
        /// </summary>
        /// <param name="tableType">Type that has defined Table attribute. 
        /// Would be used as table descriptor during queri building.</param>
        /// <param name="data">Object that contain's fields that would be writed to data base. 
        /// Affected only fields and properties with defined Column attribute.</param>
        /// <param name="error">Error faces during operation.</param>
        /// <returns>Generated command or null if failed.</returns>
        public DbCommand GenerateSetToTableCommand(Type tableType, object data, out string error)
        {
            #region Validate entry data
            // Check is SQL operator exist.
            if (Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
            }

            // Loking for table descriptor.
            if(!Table.TryToGetTableAttribute(tableType, out Table tableDescriptor, out error))
            {
                return null;
            }
            #endregion

            #region Members detection
            // Detect memebers on objects that contain columns definition.
            List<MemberInfo> members = AttributesHandler.FindMembersWithAttribute<Column>(data.GetType()).ToList();

            // Drop set ignore columns.
            members = AttributesHandler.FindMembersWithoutAttribute<SetQueryIgnore>(members).ToList();

            // Drop virtual generated columns.
            bool NotVirtual(MemberInfo member)
            {
                return !(member.GetCustomAttribute<IsGenerated>() is IsGenerated isGenerated) ||
                    isGenerated.mode != IsGenerated.Mode.Virual;
            };
            members = AttributesHandler.FindMembersWithoutAttribute<IsGenerated>(members, NotVirtual).ToList();

            // Trying to detect member with defined isAutoIncrement attribute that has default value.
            MemberInfo autoIncrementMember = null;
            try
            {
                autoIncrementMember = IsAutoIncrement.GetIgnorable(ref data, members);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
            // Remove ignorable.
            if (autoIncrementMember != null)
            {
                members.Remove(autoIncrementMember);
            }

            // Find our not key elements.
            IEnumerable<MemberInfo> membersNK = AttributesHandler.FindMembersWithoutAttribute<IsPrimaryKey>(members);
            #endregion

            #region Generating command
            // Command that can be executed on the server.
            DbCommand command = Active.NewCommand();

            // Set values as local params.
            Column.MembersDataToCommand(ref data, ref command, members);

            // Getting metas.
            Column.MembersToMetaLists(members, out List<Column> membersColumns, out List<string> membersVars);
            Column.MembersToMetaLists(membersNK, out List<Column> membersNKColumns, out List<string> membersNKVars);

            string commadText = "";
            commadText += "INSERT INTO `" + tableDescriptor.schema + "`.`" + tableDescriptor.table + "`\n";
            commadText += "\t\t(" + SqlOperatorHandler.CollectionToString(membersColumns) + ")\n";
            commadText += "\tVALUES\n";
            commadText += "\t\t(" + SqlOperatorHandler.CollectionToString(membersVars) + ")\n";
            commadText += "\tON DUPLICATE KEY UPDATE\n";
            commadText += "\t\t" + SqlOperatorHandler.ConcatFormatedCollections(membersNKColumns, membersNKVars, '\0') + ";\n";

            command.CommandText = commadText;
            #endregion

            error = null;
            return command;
        }

        /// <summary>
        /// Creating request that setting up data from object to data base server acording to attributes.
        /// </summary>
        /// <typeparam name="T">Type that has defined Table attribute. Would be used as table descriptor during queri building.</typeparam>
        /// <param name="data">Object that contain's fields that would be writed to data base. 
        /// Affected only fields and properties with defined Column attribute.</param>
        /// <param name="error">Error faces during operation.</param>
        /// <returns>Result of operation.</returns>
        public bool SetToTable<T>(object data, out string error)
        {
            // Retrun true if query was success, false if rows not affected.
            return SetToTable(typeof(T), data, out error);
        }

        /// <summary>
        /// Creating request that setting up data from object to data base server acording to attributes.
        /// </summary>
        /// <param name="tableType">Type that has defined Table attribute. Would be used as table descriptor during queri building.</param>
        /// <param name="data">Object that contain's fields that would be writed to data base. 
        /// Affected only fields and properties with defined Column attribute.</param>
        /// <param name="error">Error faces during operation.</param>
        /// <returns>Result of operation.</returns>
        public bool SetToTable(Type tableType, object data, out string error)
        {
            // Generate command
            DbCommand command = GenerateSetToTableCommand(tableType, data, out error);

            // Drop if error has been occured.
            if (!string.IsNullOrEmpty(error))
            {
                return false;
            }

            #region Execute command
            // Opening connection to DB srver.
            if (!Active.OpenConnection(out error))
            {
                return false;
            }

            // Executing command.
            command.Connection = Active.Connection;

            int affectedRowsCount;
            try
            {
                affectedRowsCount = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Query not exeuted. Query:\n" + command.CommandText + "\n\nDetails:\n" + ex.Message);
            }

            // Closing connection.
            Active.CloseConnection();
            #endregion

            // Retrun true if query was success, false if rows not affected.
            return affectedRowsCount > 0;
        }

        /// <summary>
        /// Creating request that setting up data from object to data base server acording to attributes.
        /// </summary>
        /// <typeparam name="T">Type that has defined Table attribute. 
        /// <param name="cancellationToken">Token that can terminate operation.</param>
        /// Would be used as table descriptor during queri building.</typeparam>
        /// <param name="data">Object that contains fields that would be writed to data base. 
        /// Affected only fields and properties with defined Column attribute.</param>
        public async Task SetToTableAsync<T>(CancellationToken cancellationToken, object data)
        {
            await SetToObjectAsync(typeof(T), cancellationToken, data);
        }

        /// <summary>
        /// Creating request that setting up data from object to data base server acording to attributes.
        /// </summary>
        /// <param name="tableType">Type that has defined Table attribute
        /// Would be used as table descriptor during query building.</param>
        /// <param name="cancellationToken">Token that can terminate operation.</param>
        /// <param name="data">Object that contains fields that would be writed to data base. 
        /// Affected only fields and properties with defined Column attribute.</param>
        public async Task SetToTableAsync(Type tableType, CancellationToken cancellationToken, object data)
        {
            // Generate command
            DbCommand command = GenerateSetToTableCommand(tableType, data, out string error);

            // Drop if error has been occured.
            if (!string.IsNullOrEmpty(error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(data, "Commnad generation failed. Details:\n" + error);
                return;
            }

            #region Execute command
            // Opening connection to DB srver.
            if (!Active.OpenConnection(out error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(data, "Connection failed.\n" + error);
                return;
            }

            // Executing command.
            command.Connection = Active.Connection;

            int affectedRowsCount;
            try
            {
                affectedRowsCount = await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception("Query not exeuted. Query:\n" + command.CommandText + "\n\nDetails:\n" + ex.Message);
            }

            // Closing connection.
            Active.CloseConnection();
            #endregion

            // Log if command failed.
            if (affectedRowsCount == 0)
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(data, "Query not affect any row.\n\n" + command.CommandText);
            }
        }
        #endregion
    }
}

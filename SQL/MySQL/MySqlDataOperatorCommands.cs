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
using UniformDataOperator.Sql.Tables.Attributes;
using UniformDataOperator.Sql.Tables.Attributes.Modifiers;

namespace UniformDataOperator.Sql.MySql
{
    /// <summary>
    /// Operator that provides possibility to operate data on MySQL data base server.
    /// </summary>
    public partial class MySqlDataOperator : ISqlOperator
    { 
        /// <summary>
        /// Trying to set schema to databases server in case if shema not exist.
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

            // Creating shema if not exist.
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
            Active.ExecuteNonQuery(command);

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
        /// <typeparam name="T">Type that has defined Table attribute. Would be used as table descriptor during queri building.</typeparam>
        /// <param name="data">Object that contain's fields that would be writed to data base. 
        /// Affected only fields and properties with defined Column attribute.</param>
        /// <param name="error">Error faces during operation.</param>
        /// <returns>Generated command or null if failed.</returns>
        public DbCommand GenerateSetTotableCommand<T>(object data, out string error)
        {
            #region Validate entry data
            // Check is SQL operator exist.
            if (Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
            }

            // Drop if not table descriptor.
            if (!AttributesHandler.TryToGetAttribute<Table>(typeof(T), out Table tableDesciptor))
            {
                error = "Not defined Table attribute for target type.";
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
            DbCommand command = Active.NewCommand;

            // Set values as local params.
            Column.MembersDataToCommand(ref data, ref command, members);

            // Getting metas.
            Column.MembersToMetaLists(members, out List<Column> membersColumns, out List<string> membersVars);
            Column.MembersToMetaLists(membersNK, out List<Column> membersNKColumns, out List<string> membersNKVars);

            string commadText = "";
            commadText += "INSERT INTO " + tableDesciptor.shema + "." + tableDesciptor.table + "\n";
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
            // Generate command
            DbCommand command = GenerateSetTotableCommand<T>(data, out error);

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
        public async void SetToTableAsync<T>(CancellationToken cancellationToken, object data)
        {
            // Generate command
            DbCommand command = GenerateSetTotableCommand<T>(data, out string error);

            // Drop if error has been occured.
            if (!string.IsNullOrEmpty(error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(Active, "Commnad generation failed. Details:\n" + error);
                return;
            }

            #region Execute command
            // Opening connection to DB srver.
            if (!Active.OpenConnection(out error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(Active, "Connection failed.\n" + error);
                return;
            }

            // Executing command.
            command.Connection = Active.Connection;

            int affectedRowsCount;
            try
            {
                affectedRowsCount = await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Query not exeuted. Query:\n" + command.CommandText + "\n\nDetails:\n" + ex.Message);
            }

            // Closing connection.
            Active.CloseConnection();
            #endregion

            // Log if command failed.
            if(affectedRowsCount == 0)
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(Active, "Query not affect any row.\n\n" + command.CommandText);
            }
        }
        #endregion

        #region Auto read from DB to object
        /// <summary>
        /// Trying to generate command that would request objects members from server.
        /// </summary>
        /// <typeparam name="T">Type that has defined Table attribute. 
        /// <param name="obj">Target object that cantains described primary keys, 
        /// that would be used during query generation.</param>
        /// <param name="error">Error faces during operation.</param>
        /// <param name="members"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static DbCommand GenerateSetToObjectCommand<T>(object obj, out string error, out List<MemberInfo> members, params string[] columns)
        {
            members = null;

            #region Validate entry data
            // Check is SQL operator exist.
            if (Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
            }

            // Drop if not table descriptor.
            if (!AttributesHandler.TryToGetAttribute<Table>(typeof(T), out Table tableDesciptor))
            {
                error = "Not defined Table attribute for target type.";
                return null;
            }

            // Drop if object not contains data.
            if (obj == null)
            {
                error = "Target object is null and can't be processed. Operation declined.";
                return null;
            }
            #endregion

            #region Mapping
            // Get target type map.
            members = AttributesHandler.FindMembersWithAttribute<Column>(typeof(T)).ToList();
            // Trying to detect member with defined isAutoIncrement attribute that has default value.
            MemberInfo autoIncrementMember;
            try
            {
                autoIncrementMember = IsAutoIncrement.GetIgnorable(ref obj, members);
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

            // Looking for primary keys.
            IEnumerable<MemberInfo> membersPK = AttributesHandler.FindMembersWithAttribute<IsPrimaryKey>(members);
            Column.MembersToMetaLists(membersPK, out List<Column> membersPKColumns, out List<string> membersPKVars);

            // Looking for not key elements.
            IEnumerable<MemberInfo> membersNK = AttributesHandler.FindMembersWithoutAttribute<IsPrimaryKey>(members);
            Column.MembersToMetaLists(membersNK, out List<Column> membersNKColumns, out List<string> _);
            #endregion

            #region Generate SQL query
            // Init query.
            DbCommand command = SqlOperatorHandler.Active.NewCommand;
            string query = "SELECT ";
            query += (membersNKColumns.Count == 0 ? "*" : SqlOperatorHandler.CollectionToString(membersNKColumns)) + "\n";
            query += "FROM " + tableDesciptor.shema + "." + tableDesciptor.table + "\n";
            query += "WHERE " + SqlOperatorHandler.ConcatFormatedCollections(membersPKColumns, membersPKVars) + "\n";
            query += "LIMIT 1;\n";

            // Add values as params of command.
            foreach (MemberInfo pk in membersPK)
            {
                command.Parameters.Add(
                    Active.MemberToParameter(
                            AttributesHandler.GetValue(obj, pk),
                            pk.GetCustomAttribute<Column>()
                        )
                    );
            }

            // Sign query to commandd.
            command.CommandText = query;
            #endregion

            error = null;
            return command;
        }

        /// <summary>
        /// Setting data from DB Data reader to object by using column map described at object Type.
        /// Auto-generate SQL query and request coluns data relative to privary keys described in object.
        /// </summary>
        /// <typeparam name="T">Type that has defined Table attribute. 
        /// <param name="obj">Target object that cantains described primary keys, 
        /// that would be used during query generation.</param>
        /// <param name="error">Error faces during operation.</param>
        /// <param name="columns">List of requested columns that would included to SQL query.</param>
        /// <returns>Result of operation.</returns>
        public bool SetToObject<T>(object obj, out string error, params string[] columns)
        {
            // Generate command.
            DbCommand command = GenerateSetToObjectCommand<T>(
                obj,
                out error,
                out List<MemberInfo> members,
                columns);

            // Drop if error has been occured.
            if (!string.IsNullOrEmpty(error))
            {
                error = "Commnad generation failed. Details:\n" + error;
                return false;
            }

            #region Execute query
            // Opening connection to DB srver.
            if (!Active.OpenConnection(out error))
            {
                error = "Connection failed. Details:\n" + error;
                return false;
            }
            command.Connection = SqlOperatorHandler.Active.Connection;

            // Await for reader.
            DbDataReader reader = command.ExecuteReader();

            // Drop if DbDataReader is invalid.           
            if (reader == null || reader.IsClosed)
            {
                error = "DbDataReader is null or closed. Operation declined.";
                return false;
            }

            // Try to apply data from reader to object.
            bool result = SqlOperatorHandler.DatabaseDataToObject(reader, members, obj, out error);

            // Closing connection.
            Active.CloseConnection();
            #endregion

            return result;
        }

        /// <summary>
        /// Setting data from DB Data reader to object by using column map described at object Type.
        /// Auto-generate SQL query and request coluns data relative to privary keys described in object.
        /// </summary>
        /// <typeparam name="T">Type that has defined Table attribute. 
        /// <param name="cancellationToken">Token that can terminate operation.</param>
        /// Would be used as table descriptor during queri building.</typeparam>
        /// <param name="obj">Target object that cantains described primary keys, 
        /// that would be used during query generation.</param>
        /// <param name="columns">List of requested columns that would included to SQL query.</param>
        public async void SetToObjectAsync<T>(CancellationToken cancellationToken, object obj, params string[] columns)
        {
            // Generate command.
            DbCommand command = GenerateSetToObjectCommand<T>(
                obj, 
                out string error, 
                out List<MemberInfo> members, 
                columns);

            // Drop if error has been occured.
            if (!string.IsNullOrEmpty(error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(Active, "Commnad generation failed. Details:\n" + error);
                return;
            }

            #region Execute query
            // Opening connection to DB srver.
            if (!Active.OpenConnection(out error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(Active, "Connection failed. Details:\n" + error);
                return;
            }
            command.Connection = SqlOperatorHandler.Active.Connection;
            
            // Await for reader.
            DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            
            // Drop if DbDataReader is invalid.           
            if (reader == null || reader.IsClosed)
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(Active,
                    "DbDataReader is null or closed. Operation declined.");
                return;
            }

            // Try to apply data from reader to object.
            if (!SqlOperatorHandler.DatabaseDataToObject(reader, members, obj, out error))
            {
                // Log error.
                SqlOperatorHandler.InvokeSQLErrorOccured(Active, error);
            }

            // Closing connection.
            Active.CloseConnection();
            #endregion
        }
        #endregion
    }
}

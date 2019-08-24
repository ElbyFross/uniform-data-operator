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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TA = UniformDataOperator.SQL.Tables.Attributes;

namespace UniformDataOperator.SQL
{
    /// <summary>
    /// Contains base catalog of uniform queries that strongly simplify managing of the data base.
    /// </summary>
    public static class SQLOperatorHandler
    {
        /// <summary>
        /// Contains las operator that asing itself to handler as active one.
        /// </summary>
        public static ISQLOperator Active { get; set; }

        /// <summary>
        /// Trying to set schema to databases server in case if shema not exist.
        /// </summary>
        /// <param name="shemaName">Name of the schema that would be used\created.</param>
        /// <returns></returns>
        public static bool ActivateSchema(string shemaName)
        {
            // Check is SQL operator exist.
            if (Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
            }

            // Variable that would contain SQL comand.
            string command = "";

            // Creating shema if not exist.
            command += "CREATE SCHEMA IF NOT EXISTS `" + shemaName + "` DEFAULT CHARACTER SET utf8 ;\n";

            // Setting schema as target.
            command += "USE `datum-point` ;";

            // Tring to open connection to the server.
            if (!Active.OpenConnection())
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
        /// Return command that would allow to create table by descriptor.
        /// </summary>
        /// <param name="tableDescriptor"></param>
        /// <returns></returns>
        public static string GenerateCreateTableCommand(Type sourceType)
        {
            if(!AttributesHandler.TryToGetAttribute<TA.Table>(sourceType, out TA.Table table))
            {
                // Drop cause not a table descriptor.
                return "";
            }
            
            // Variable that would contain SQL comand.
            string command = "";
            command += "CREATE TABLE IF NOT EXISTS `" + table.shema + "`.`" + table.table + "` (\n";

            IEnumerable<MemberInfo> columns = AttributesHandler.FindMembersWithAttribute<TA.Column>(sourceType);

            #region Declere columns
            string colCommand = "";
            foreach (MemberInfo member in columns)
            {
                AttributesHandler.TryToGetAttribute<TA.Column>(member, out TA.Column column);
                if (!string.IsNullOrEmpty(colCommand))
                {
                    colCommand += ",\n";
                }
                colCommand += column.ColumnDeclarationCommand(member);
            }
            command += colCommand;
            #endregion

            #region Primary keys
            // Build PKs substring string.
            string subPkCommand = "";
            foreach (MemberInfo cMeta in columns)
            {
                if (AttributesHandler.TryToGetAttribute<TA.IsPrimaryKey>(cMeta, out TA.IsPrimaryKey isPrimaryKey))
                {
                    if (!string.IsNullOrEmpty(subPkCommand))
                    {
                        command += ", ";
                    }

                    AttributesHandler.TryToGetAttribute<TA.Column>(cMeta, out TA.Column column);
                    subPkCommand += "`" + column.title + "`";
                }
            }

            // Add to command command if pks exist.
            command += subPkCommand.Length > 0 ? ",\nPRIMARY KEY(" + subPkCommand + ")" : "";
            #endregion

            #region Unique indexes
            foreach (MemberInfo cMeta in columns)
            {
                if (AttributesHandler.TryToGetAttribute<TA.IsUnique>(cMeta, out TA.IsUnique isUnique))
                {
                    command += ",\n";
                    command += isUnique.UniqueIndexDeclarationCommand(cMeta);
                }
            }
            #endregion

            #region FK indexes
            foreach (MemberInfo cMeta in columns)
            {
                if (AttributesHandler.TryToGetAttribute<TA.IsForeignKey>(cMeta, out TA.IsForeignKey isForeignKey))
                {
                    command += ",\n";

                    AttributesHandler.TryToGetAttribute<TA.Column>(cMeta, out TA.Column column);
                    command += TA.IsForeignKey.FKIndexDeclarationCommand(column, isForeignKey, table.table);
                }
            }
            #endregion

            #region Constraints
            foreach (MemberInfo cMeta in columns)
            {
                if (AttributesHandler.TryToGetAttribute<TA.IsForeignKey>(cMeta, out TA.IsForeignKey isForeignKey))
                {
                    command += ",\n";

                    AttributesHandler.TryToGetAttribute<TA.Column>(cMeta, out TA.Column column);
                    command += TA.IsForeignKey.ConstrainDeclarationCommand(column, isForeignKey, table.table);
                }
            }
            #endregion

            command += ")\n";
            command += "ENGINE = " + (string.IsNullOrEmpty(table.engine) ? "InnoDB" : table.engine) + ";";

            return command;
        }

        /// <summary>
        /// Trying to set table if required.
        /// </summary>
        /// <param name="tableDescriptor">Type that would be trying to recreate on your SQL server. 
        /// Must has defined UniformDataOperator.SQL.Tables.Attributes.Table attribute.</param>
        /// <returns>Success of operation.</returns>
        public static bool TrySetTable(Type tableDescriptor)
        {
            // Check is SQL operator exist.
            if (Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
            }

            // Drop if not table descriptor.
            if(!AttributesHandler.HasAttribute<TA.Table>(tableDescriptor))
            {
                return false;
            }

            // Get command.
            string command = GenerateCreateTableCommand(tableDescriptor);

            #region Execute command
            // Tring to open connection to the server.
            if (!Active.OpenConnection())
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
            #endregion
        }
    }
}

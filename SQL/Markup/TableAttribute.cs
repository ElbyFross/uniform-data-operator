﻿//Copyright 2019 Volodymyr Podshyvalov
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
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UniformDataOperator.AssembliesManagement;

namespace UniformDataOperator.Sql.Markup
{
    /// <summary>
    /// Attribute that causes auto-generation of a table on the SQL server based on members declared in the relative class or structure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class TableAttribute : Attribute
    { 
        /// <summary>
        /// Name of the holding schema.
        /// </summary>
        public string schema;

        /// <summary>
        /// Name of the table.
        /// </summary>
        public string table;

        /// <summary>
        /// Name of the database engine.
        /// </summary>
        public string engine = "InnoDB";

        /// <summary>
        /// Configurate SQL table.
        /// </summary>
        /// <param name="schema">Name of foreign schema.</param>
        /// <param name="table">Name of foreign table.</param>
        public TableAttribute(string schema, string table)
        {
            this.schema = schema;
            this.table = table;
        }

        /// <summary>
        /// Configurate SQL table.
        /// </summary>
        /// <param name="schema">Name of foreign schema.</param>
        /// <param name="table">Name of foreign table.</param>
        /// <param name="engine">Name of the database engine.</param>
        public TableAttribute(string schema, string table, string engine)
        {
            this.schema = schema;
            this.table = table;
            this.engine = engine;
        }


        /// <summary>
        /// Return command that would allow to create table by descriptor.
        /// </summary>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        public static string GenerateCreateTableCommand(Type sourceType)
        {
            // Loking for table descriptor.
            if (!TableAttribute.TryToGetTableAttribute(sourceType, out TableAttribute tableDescriptor, out string error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(sourceType, error);
                return null;
            }

            // Variable that would contain SQL comand.
            string command = "";
            command += "CREATE TABLE IF NOT EXISTS `" + tableDescriptor.schema + "`.`" + tableDescriptor.table + "` (\n";

            IEnumerable<MemberInfo> columns = MembersHandler.FindMembersWithAttribute<ColumnAttribute>(sourceType);

            #region Declere columns
            string colCommand = "";
            foreach (MemberInfo member in columns)
            {
                if (!string.IsNullOrEmpty(colCommand))
                {
                    colCommand += ",\n";
                }
                colCommand +=  SqlOperatorHandler.Active.ColumnDeclarationCommand(member);
            }
            command += colCommand;
            #endregion

            #region Primary keys
            // Build PKs substring string.
            string subPkCommand = "";
            foreach (MemberInfo cMeta in columns)
            {
                if (MembersHandler.TryToGetAttribute<IsPrimaryKeyAttribute>(cMeta, out IsPrimaryKeyAttribute isPrimaryKey))
                {
                    if (!string.IsNullOrEmpty(subPkCommand))
                    {
                        subPkCommand += ", ";
                    }

                    MembersHandler.TryToGetAttribute<ColumnAttribute>(cMeta, out ColumnAttribute column);
                    subPkCommand += "`" + column.title + "`";
                }
            }

            // Add to command command if pks exist.
            command += subPkCommand.Length > 0 ? ",\nPRIMARY KEY(" + subPkCommand + ")" : "";
            #endregion

            #region Unique indexes
            foreach (MemberInfo cMeta in columns)
            {
                if (MembersHandler.TryToGetAttribute<IsUniqueAttribute>(cMeta, out IsUniqueAttribute isUnique))
                {
                    command += ",\n";
                    command += isUnique.UniqueIndexDeclarationCommand(cMeta);
                }
            }
            #endregion

            #region FK indexes
            IsForeignKeyAttribute.DropIndexator();
            foreach (MemberInfo cMeta in columns)
            {
                string decleration = IsForeignKeyAttribute.FKIndexDeclarationCommand(cMeta, tableDescriptor.table);
                if (!string.IsNullOrEmpty(decleration))
                {
                    command += ",\n" + decleration;
                }
            }
            #endregion

            #region Constraints
            IsForeignKeyAttribute.DropIndexator();
            foreach (MemberInfo cMeta in columns)
            {
                string decleration = IsForeignKeyAttribute.ConstrainDeclarationCommand(cMeta, tableDescriptor.table);
                if (!string.IsNullOrEmpty(decleration))
                {
                    command += ",\n" + decleration;
                }
            }
            #endregion

            command += ")\n";

            command += "ENGINE = " + (string.IsNullOrEmpty(tableDescriptor.engine) ? "InnoDB" : tableDescriptor.engine) + ";";

            return command;
        }

        /// <summary>
        /// Trying to set some tables to SQL server.
        /// Existed ones would be skiped.
        /// Not updating alter columns.
        /// </summary>
        /// <param name="disableSQLChecks">Disable check of data itegrity during command.</param>
        /// <param name="error">Error cased during operation. Null if al passed without exceptions.</param>
        /// <param name="tableDescriptors">Types with defined "Table" attribute.</param>
        /// <returns>Result of operation.</returns>
        public static bool TrySetTables(bool disableSQLChecks, out string error, params Type[] tableDescriptors)
        {
            // Check is SQL operator exist.
            if (SqlOperatorHandler.Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
            }

            // Get command for all tables.
            string command = "";
            foreach (Type type in tableDescriptors)
            {
                if (command != "")
                {
                    command += "\n\n";
                }

                // Drop if not table descriptor.
                if (!MembersHandler.HasAttribute<TableAttribute>(type))
                {
                    error = "Not defined Table attribute for target type.";
                    return false;
                }

                try
                {
                    command += GenerateCreateTableCommand(type);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }

            #region Execute command
            // Tring to open connection to the server.
            if (!SqlOperatorHandler.Active.OpenConnection(out error))
            {
                // Inform about fail.
                return false;
            }

            // Disabling checks if requested.
            if(disableSQLChecks)
            {
                command = SqlOperatorHandler.Active.DisableSqlChecks(command);
            }

            try
            {
                // Execute command
                SqlOperatorHandler.Active.NewCommand(command)?.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                error = ex.Message + "\n\nSQL query:\n" + command;

                // Close connection after finish.
                SqlOperatorHandler.Active.CloseConnection();

                return false;
            }
            
            // Close connection after finish.
            SqlOperatorHandler.Active.CloseConnection();
            
            // Confirm success.
            return true;
            #endregion
        }

        /// <summary>
        /// Trying to set table if required.
        /// Not updating alter columns.
        /// </summary>
        /// <param name="disableSQLChecks">Disable check of data itegrity during command.</param>
        /// <param name="tableDescriptor">Type that would be trying to recreate on your SQL server. 
        /// Must has defined UniformDataOperator.Sql.Attributes.Table attribute.</param>
        /// <param name="error">Error faces during operation.</param>
        /// <returns>Result of operation.</returns>
        public static bool TrySetTable(bool disableSQLChecks, Type tableDescriptor, out string error)
        {
            // Check is SQL operator exist.
            if (SqlOperatorHandler.Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
            }

            // Drop if not table descriptor.
            if (!MembersHandler.HasAttribute<TableAttribute>(tableDescriptor))
            {
                error = "Not defined Table attribute for target type.";
                return false;
            }

            // Get command.
            string command;
            try
            {
                command = GenerateCreateTableCommand(tableDescriptor);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            #region Execute command
            // Tring to open connection to the server.
            if (!SqlOperatorHandler.Active.OpenConnection(out error))
            {
                // Inform about fail.
                return false;
            }

            // Disabling checks if requested.
            if (disableSQLChecks)
            {
                command = SqlOperatorHandler.Active.DisableSqlChecks(command);
            }

            // Execute command
            SqlOperatorHandler.Active.NewCommand(command)?.ExecuteNonQuery();

            // Close connection after finish.
            SqlOperatorHandler.Active.CloseConnection();

            // Confirm success.
            return true;
            #endregion
        }

        /// <summary>
        /// Looking for members that's column's titles included to array.
        /// </summary>
        /// <param name="members">List of members that will be used for columns search.</param>
        /// <param name="columnTitles">Array that contais column's titles that would be looking among members.</param>
        /// <returns>List with suitable members.</returns>
        public static List<MemberInfo>FindMembersByColumns(IEnumerable<MemberInfo> members, params string[] columnTitles)
        {
            // List that would contains all member that has the column title requested in columnTitles array.
            List<MemberInfo> result = new List<MemberInfo>();

            // Convert to list to allow fast removing.
            List<string> ctl = columnTitles.ToList();

            // Check every provided member for existing in requesteed list.
            foreach(MemberInfo mi in members)
            {
                // Getting column descriptor.
                if(mi.GetCustomAttribute<ColumnAttribute>() is ColumnAttribute column)
                {
                    // Checl every request.
                    for(int i = 0; i < ctl.Count; i++)
                    {
                        // Check if member included to request list.
                        if(column.title == ctl[i])
                        {
                            result.Add(mi);     // Add member to output list.
                            ctl.RemoveAt(i);    // Remove found element.
                            break;              // Drop serching.
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Looking for valid Table attribute. Chack all overridings and return correct descriptor.
        /// </summary>
        /// <param name="tableType">Type that contains defined Table attribute.</param>
        /// <param name="tableDescriptor">Output table descriptor.</param>
        /// <param name="error">Error message in cvase if occured. Null in other case.</param>
        /// <returns>A result of operation.</returns>
        public static bool TryToGetTableAttribute(Type tableType, out TableAttribute tableDescriptor, out string error)
        {
            // Drop if not table descriptor.
            if (!MembersHandler.TryToGetAttribute<TableAttribute>(tableType, out tableDescriptor))
            {
                error = "Not defined Table attribute for target type.";
                tableDescriptor = null;
                return false;
            }

            // Try to find overriding attribute for table descriptor.
            if (Modifiers.DBPathOverrideAttribute.TryToGetValidOverride<TableAttribute>(
                tableType,
                out Modifiers.DBPathOverrideAttribute tableOverrider))
            {
                tableDescriptor.schema = tableOverrider.schema ?? tableDescriptor.schema;
                tableDescriptor.table = tableOverrider.table ?? tableDescriptor.table;
            }

            error = null;
            return true;
        }
    }
}

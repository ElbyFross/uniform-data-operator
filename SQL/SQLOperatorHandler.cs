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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static string GenerateCreateTableCommand(Tables.ISQLTable tableDescriptor)
        {
            // Validate data.
            Tables.TableColumnMeta[] columnMetas = tableDescriptor.TableFields;
            if (columnMetas == null)
            {
                columnMetas = new Tables.TableColumnMeta[0];
            }

            // Variable that would contain SQL comand.
            string command = "";
            command += "CREATE TABLE IF NOT EXISTS `" + tableDescriptor.SchemaName + "`.`" + tableDescriptor.TableName + "` (\n";

            #region Declere columns
            string colCommand = "";
            foreach (Tables.TableColumnMeta cMeta in columnMetas)
            {
                if (!string.IsNullOrEmpty(colCommand))
                {
                    colCommand += ",\n";
                }
                colCommand += cMeta.ColumnDeclarationCommand();
            }
            command += colCommand;
            #endregion

            #region Primary keys
            // Build PKs substring string.
            string subPkCommand = "";
            foreach (Tables.TableColumnMeta cMeta in columnMetas)
            {
                if (cMeta.isPrimaryKey)
                {
                    if (!string.IsNullOrEmpty(subPkCommand))
                    {
                        command += ", ";
                    }
                    subPkCommand += "`" + cMeta.name + "`";
                }
            }

            // Add to command command if pks exist.
            command += subPkCommand.Length > 0 ? ",\nPRIMARY KEY(" + subPkCommand + ")" : "";
            #endregion

            #region Unique indexes
            foreach (Tables.TableColumnMeta cMeta in columnMetas)
            {
                if (cMeta.isUnique)
                {
                    command += ",\n";
                    command += cMeta.UniqueIndexDeclarationCommand();
                }
            }
            #endregion

            #region FK indexes
            foreach (Tables.TableColumnMeta cMeta in columnMetas)
            {
                if (cMeta.isForeignKey)
                {
                    command += ",\n";
                    command += cMeta.FKIndexDeclarationCommand(tableDescriptor.TableName);
                }
            }
            #endregion

            #region Constraints
            foreach (Tables.TableColumnMeta cMeta in columnMetas)
            {
                if (cMeta.isForeignKey)
                {
                    command += ",\n";
                    command += cMeta.ConstrainDeclarationCommand(tableDescriptor.TableName);
                }
            }
            #endregion

            command += ")\n";
            command += "ENGINE = " + (string.IsNullOrEmpty(tableDescriptor.TableEngine) ? "InnoDB" : tableDescriptor.TableEngine) + ";";

            return command;
        }

        /// <summary>
        /// Trying to set table if required.
        /// </summary>
        /// <param name="tableDescriptor"></param>
        /// <returns></returns>
        public static bool TrySetTable(Tables.ISQLTable tableDescriptor)
        {
            // Check is SQL operator exist.
            if (Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
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

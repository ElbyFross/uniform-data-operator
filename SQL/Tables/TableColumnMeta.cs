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

namespace UniformDataOperator.SQL.Tables
{
    /// <summary>
    /// Describe requirments to field in SQL table.
    /// </summary>
    [System.Serializable]
    public struct TableColumnMeta
    {
        /// <summary>
        /// Name of field.
        /// </summary>
        public string name;

        /// <summary>
        /// SQL like type of the field.
        /// </summary>
        public string type;

        /// <summary>
        /// Is value always not null.
        /// </summary>
        public bool isNotNull;
        
        /// <summary>
        /// Is this value must be unique by this column. 
        /// </summary>
        public bool isUnique;

        /// <summary>
        /// Is data would stored in binary format.
        /// </summary>
        public bool isBinary;

        /// <summary>
        /// Is not have signs after dot.
        /// </summary>
        public bool isUnsigned;

        /// <summary>
        /// Is value wold be willed by zero by default. Only for numerical columns.
        /// </summary>
        public bool isZeroFill;

        /// <summary>
        /// Is value of this column would incremented relative to previous one during init.
        /// </summary>
        public bool isAutoIncrement;

        #region Generated settings
        /// <summary>
        /// Is this colum is generated.
        /// </summary>
        public bool isGenerated;

        /// <summary>
        /// Stored or Virtual.
        /// </summary>
        public bool isStored;

        /// <summary>
        /// Default or Expression value.
        /// </summary>
        public string defExp;
        #endregion

        /// <summary>
        /// Commentary that would added to column.
        /// </summary>
        public string comment;

        /// <summary>
        /// Is it's primary key.
        /// </summary>
        public bool isPrimaryKey;

        #region Foreign key
        public bool isForeignKey;

        public string refSchema;

        public string refTable;

        public string refColumn;

        public string onDeleteCommand;

        public string onUpdateCommand;
        #endregion


        /// <summary>
        /// Return generated SQL command relative to init time.
        /// </summary>
        public string ColumnDeclarationCommand()
        {
            string command = "";

            command += "'" + name + "'";
            command += " " + type;
            command += isZeroFill ? " ZEROFILL" : "";
            command += isBinary ? " BINARY" : "";
            command += isUnsigned ? " UNSIGNED" : "";
            if (!string.IsNullOrEmpty(defExp))
            {
                if (isGenerated)
                {
                    command += " GENERATED ALWAYS AS(";
                    command += defExp + ") ";
                    command += (isStored ? "STORED" : "VIRTUAL");
                }
                else
                {
                    command += " DEFAULT " + defExp;
                }
            }
            if (!isGenerated)
            {
                command += " " + (isNotNull ? "NOT NULL" : "NULL");
            }
            command += isAutoIncrement ? " AUTO_INCREMENT" : "";
            return command;
        }

        /// <summary>
        /// Return index init string suitable from forgeign key suitable for this column.
        /// </summary>
        /// <param name="selfTableName"></param>
        /// <returns></returns>
        public string FKIndexDeclarationCommand(string selfTableName)
        {
            string command = "";
            if (isForeignKey)
            {
                command += "INDEX `" + FKKeyName(selfTableName) + "_idx` (`" + name + "` ASC) VISIBLE";
            }
            return command;
        }

        /// <summary>
        /// Generate fk key name related to this column.
        /// </summary>
        /// <param name="selfTableName"></param>
        /// <returns></returns>
        public string FKKeyName(string selfTableName)
        {
            return "fk_" + selfTableName + "_" + refTable + "1";
        }

        /// <summary>
        /// Return unique index init string.
        /// </summary>
        /// <returns></returns>
        public string UniqueIndexDeclarationCommand()
        {
            string command = "";
            if (isUnique)
            {
                command += "UNIQUE INDEX `" + name + "_UNIQUE` (`" + name + "` ASC) VISIBLE";
            }
            return command;
        }

        /// <summary>
        /// Generate init string from contrains related to this column.
        /// </summary>
        /// <param name="selfTableName"></param>
        /// <returns></returns>
        public string ConstrainDeclarationCommand(string selfTableName)
        {
            // Drop if not required.
            if (!isForeignKey)
            {
                return "";
            }

            string command = "";
            command += "CONSTRAINT `"+ FKKeyName(selfTableName) +"`\n";
            command += "\tFOREIGN KEY(`" + name + "`)\n";
            command += "\tREFERENCES `"+ refSchema + "`.`" + refTable + "` (`" + refColumn + "`)\n";
            command += "\tON DELETE " + (string.IsNullOrEmpty(onDeleteCommand) ? "NO ACTION" : onDeleteCommand) + "\n";
            command += "\tON UPDATE " + (string.IsNullOrEmpty(onUpdateCommand) ? "NO ACTION" : onUpdateCommand);
            return command;
        }
    }
}

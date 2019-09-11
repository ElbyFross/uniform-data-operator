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
        /// Initialize MySqlConnection.
        /// </summary>
        public void Initialize()
        {
            string connectionString;
            connectionString = 
                "SERVER=" + Server + ";" + 
                (string.IsNullOrEmpty(Database) ? "" :  "DATABASE=" + Database + ";") +
                "port=" + Port + ";" + 
                "User Id=" + UserId + ";"+
                "PASSWORD=" + Password + ";";

            connection = new MySqlConnection(connectionString);
        }

        /// <summary>
        /// Return new clear command suitable for current DB.
        /// </summary>
        public DbCommand NewCommand()
        {
            return new MySqlCommand("", connection);
        }

        /// <summary>
        /// Return new clear command suitable for current DB with included command text.
        /// </summary>
        public DbCommand NewCommand(string commandText)
        {
            return new MySqlCommand(commandText, connection);
        }

        /// <summary>
        /// Add code that disabling SQL checks during executing command.
        /// </summary>
        /// <param name="command">Target command that would be modified during operation.</param>
        /// <returns>Modified comand.</returns>
        public DbCommand DisableSqlChecks(DbCommand command)
        {
            command.CommandText = DisableSqlChecks(command.CommandText);
            return command;
        }

        /// <summary>
        /// Add code that disabling SQL checks during executing command.
        /// </summary>
        /// <param name="command">Target command that would be modified during operation.</param>
        /// <returns>Modified comand.</returns>
        public string DisableSqlChecks(string command)
        {
            //string disableCommand =
            //"SET @OLD_UNIQUE_CHECKS =@@UNIQUE_CHECKS, UNIQUE_CHECKS = 0;\n" +
            //"SET @OLD_FOREIGN_KEY_CHECKS =@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS = 0;\n" +
            //"SET @OLD_SQL_MODE =@@SQL_MODE, SQL_MODE = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';";

            //string revertCommand =
            //    "SET SQL_MODE = @OLD_SQL_MODE;\n" +
            //    "SET FOREIGN_KEY_CHECKS = @OLD_FOREIGN_KEY_CHECKS;\n" +
            //    "SET UNIQUE_CHECKS = @OLD_UNIQUE_CHECKS;";

            string disableCommand =
            "SET UNIQUE_CHECKS = 0;\n" +
            "SET FOREIGN_KEY_CHECKS = 0;";

            string revertCommand =
                "SET FOREIGN_KEY_CHECKS = 1;\n" +
                "SET UNIQUE_CHECKS = 1;";

            return disableCommand + "\n\n" + command + "\n\n" + revertCommand;
        }

        /// <summary>
        /// Validate data base table column acording to member attributes.
        /// </summary>
        /// <param name="tableDescriptor">Table meta data.</param>
        /// <param name="columnMember">Member with defined Column attribute that would be comared with </param>
        /// <returns>Result of validation.</returns>W
        public bool ValidateTableMember(Table tableDescriptor, MemberInfo columnMember)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Convert value of member to data base parameter that can be used in command.
        /// </summary>
        /// <param name="data">Value of the object that would applied to parameter.</param>
        /// <param name="column">Column attribute relative to member of data.</param>
        /// <returns>Parameter that could by used in commands to data base.</returns>
        public DbParameter MemberToParameter(object data, Column column)
        {
            return new MySqlParameter("@" + column.title, data);
        }

        /// <summary>
        /// Trying to convert DBType to specified type in string format that suitable to this database.
        /// </summary>
        /// <param name="type">Common DBType.</param>
        /// <returns>Type suitable for SQL command relative to this type of data base. 
        /// InvalidCastException in case if converting not possible.</returns>
        public string DbTypeToString(DbType type)
        {
            MySqlParameter parm = new MySqlParameter
            {
                DbType = type
            };
            MySqlDbType mySqlDbType = parm.MySqlDbType;

            switch(mySqlDbType)
            {
                case MySqlDbType.Int32:
                case MySqlDbType.Int64:
                case MySqlDbType.UInt16:
                case MySqlDbType.UInt24:
                case MySqlDbType.UInt32:
                case MySqlDbType.UInt64:
                    return "INT";

                case MySqlDbType.String:
                    return "TEXT(65535)";

                case MySqlDbType.VarChar:
                    return "VARCHAR(45)";

                default:
                    return mySqlDbType.ToString();
            }
        }
    }
}

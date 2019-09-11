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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using UniformDataOperator.Sql.Attributes;

namespace UniformDataOperator.Sql.MySql
{
    /// <summary>
    /// Operator that provides possibility to operate data on MySQL data base server.
    /// </summary>
    public partial class MySqlDataOperator : ISqlOperator
    {
        #region Single ton
        /// <summary>
        /// Active single tone instance of MySQL data provider.
        /// </summary>
        public static MySqlDataOperator Active
        {
            get
            {
                // Create new instence of MySQL data operator.
                if(!(SqlOperatorHandler.Active is MySqlDataOperator))
                {
                    SqlOperatorHandler.Active = new MySqlDataOperator();
                }
                return (MySqlDataOperator)SqlOperatorHandler.Active;
            }
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Server's ip.
        /// </summary>
        public string Server { get; set; } = "127.0.0.1";

        /// <summary>
        /// Port for server access.
        /// </summary>
        public int Port { get; set; } = 3306;

        /// <summary>
        /// Database's name.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// User for connection.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// User's password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Connection to DB.
        /// </summary>
        public DbConnection Connection {
            get
            {
                if(connection == null)
                {
                    Initialize();
                }

                return connection;
            }
        }
        #endregion

        #region Private fields
        /// <summary>
        /// Object that managing connection with DB.
        /// </summary>
        private MySqlConnection connection;
        #endregion
    }
}

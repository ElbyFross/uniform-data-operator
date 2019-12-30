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
using UniformDataOperator.Sql.Markup;

namespace UniformDataOperator.Sql.MySql
{
    /// <summary>
    /// Operator that provides possibility to operate data on MySQL database server.
    /// </summary>
    public partial class MySqlDataOperator : ISqlOperator
    {
        /// <summary>
        /// Opening connection to SQL server.
        /// </summary>
        /// <param name="error">Faced error. Null if passed success.</param>
        /// <returns>Result of connection.</returns>
        public bool OpenConnection(out string error)
        {
            error = null;
            try
            {
                Connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                error = "Error not defined.";

                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        error = "Cannot connect to server. Contact administrator.\n" + ex.Message;
                        break;

                    case 1045:
                        error = "Invalid username/password, please try again.\n" + ex.Message;
                        break;
                }

                Console.WriteLine(error);
                return false;
            }
        }

        /// <summary>
        /// Closing connection to SQL server.
        /// </summary>
        /// <returns>Result of connection closing.</returns>
        public bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}

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
using UniformDataOperator.Sql.Tables.Attributes;

namespace UniformDataOperator.Sql.MySql
{
    /// <summary>
    /// Operator that provides possibility to operate data on MySQL data base server.
    /// </summary>
    public partial class MySqlDataOperator : ISqlOperator
    {       
        /// <summary>
        /// Sending SQL query to server.
        /// </summary>
        /// <param name="query"></param>
        public void ExecuteNonQuery(string query)
        {
            #region Validate connection
            // Check connection
            bool connectionOpened = connection != null ? 
                connection.State == System.Data.ConnectionState.Open : 
                false;
            if (!connectionOpened)
            {
                // Log error.
                Console.WriteLine("Connection to MySQL database not opened. Operation rejected.");

                // Terminate operation.
                return;
            }
            #endregion

            //create command and assign the query and connection from the constructor
            MySqlCommand cmd = new MySqlCommand(query, connection);

            //Execute command
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                throw new Exception("Invalid query. Query:\n" + query + "\n\nDetails:\n" + ex.Message);
            }
        }

        /// <summary>
        /// Serching for first entry suitable to query and return as result.
        /// </summary>
        /// <param name="query"></param>
        /// <returns>Recived data.</returns>
        public object ExecuteScalar(string query)
        {
            #region Validate connection
            // Check connection
            bool connectionOpened = connection != null ? 
                connection.State == System.Data.ConnectionState.Open : 
                false;
            if (!connectionOpened)
            {
                // Log error.
                Console.WriteLine("Connection to MySQL database not opened. Operation rejected.");

                // Terminate operation.
                return null;
            }
            #endregion

            //Create Mysql Command
            MySqlCommand cmd = new MySqlCommand(query, connection);

            //Execute command
            return cmd.ExecuteScalar();
        }

        /// <summary>
        /// Execute complex data reader.
        /// </summary>
        /// <param name="query">SQL query that would be shared to server.</param>
        /// <returns>Data reader with recived data.</returns>
        public DbDataReader ExecuteReader(string query)
        {
            #region Validate connection
            // Check connection
            bool connectionOpened = connection != null ? connection.State == System.Data.ConnectionState.Open : false;
            if (!connectionOpened)
            {
                // Log error.
                Console.WriteLine("Connection to MySQL database not opened. Operation rejected.");

                // Terminate operation.
                return null;
            }
            #endregion

            //Create Command
            MySqlCommand cmd = new MySqlCommand(query, connection);

            //Create a data reader and Execute the command
            MySqlDataReader dataReader = cmd.ExecuteReader();

            return dataReader;
        }

        /// <summary>
        /// Try to return count by query.
        /// </summary>
        /// <param name="query">SQL query that would send to server.</param>
        /// <returns>Count. -1 by default.</returns>
        public int Count(string query)
        {
            #region Validate connection
            // Check connection
            bool connectionOpened = connection != null ? 
                connection.State == System.Data.ConnectionState.Open : 
                false;
            if (!connectionOpened)
            {
                // Log error.
                Console.WriteLine("Connection to MySQL database not opened. Operation rejected.");

                // Terminate operation.
                return -1;
            }
            #endregion

            //Create Mysql Command
            MySqlCommand cmd = new MySqlCommand(query, connection);

            //ExecuteScalar will return one value
            int count = int.Parse(cmd.ExecuteScalar() + "");

            return count;
        }
    }
}

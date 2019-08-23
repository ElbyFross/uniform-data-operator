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
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace UniformDataOperator.SQL.MySQL
{
    /// <summary>
    /// Operator that provides possibility to operate data on MySQL data base server.
    /// </summary>
    public class MySQLDataOperator : ISQLOperator
    {
        #region Single ton
        /// <summary>
        /// Active single tone instance of MySQL data provider.
        /// </summary>
        public static MySQLDataOperator Active
        {
            get
            {
                // Create new instence of MySQL data operator.
                if(!(_Active is MySQLDataOperator))
                {
                    _Active = new MySQLDataOperator();
                }

                // Set current as active.
                SQLOperatorHandler.Active = _Active;
                return _Active;
            }
        }
        private static MySQLDataOperator _Active;
        #endregion

        #region Public properties
        /// <summary>
        /// Server's ip.
        /// </summary>
        public string Server { get; set; }

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
        #endregion

        #region Private fields
        /// <summary>
        /// Object that managing connection with DB.
        /// </summary>
        private MySqlConnection connection;
        #endregion


        /// <summary>
        /// Initialize MySqlConnection.
        /// </summary>
        public void Initialize()
        {
            string connectionString;
            connectionString = "SERVER=" + Server + ";" + "DATABASE=" +
            Database + ";" + "UID=" + UserId + ";" + "PASSWORD=" + Password + ";";

            connection = new MySqlConnection(connectionString);
        }

        #region Connection API
        /// <summary>
        /// Opening connection to SQL server.
        /// </summary>
        /// <returns>Result of connection.</returns>
        public bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
                        Console.WriteLine("Cannot connect to server.  Contact administrator");
                        break;

                    case 1045:
                        Console.WriteLine("Invalid username/password, please try again");
                        break;
                }
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
        #endregion

        #region Queries API
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
            cmd.ExecuteNonQuery();
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
        #endregion

        #region Backup API
        /// <summary>
        /// Backuping data base to sql file in directory.
        /// Name would be generated by timestamp.
        /// </summary>
        /// <param name="directory">Directory that would store the backup file.</param>
        public void Backup(string directory)
        {
            #region Path generation
            string path;
            DateTime Time = DateTime.Now;

            // Add directory spliter.
            if (!directory[directory.Length - 1].Equals('\\'))
            {
                directory += "\\";
            }

            path = directory + "MySqlBackup" + Time.Year + "-" + Time.Month + "-" + Time.Day +
                "-" + Time.Hour + "-" + Time.Minute + "-" + Time.Second + "-" + Time.Millisecond + ".sql";
            #endregion


            #region Start MySQL Dump utilit
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "mysqldump",
                RedirectStandardInput = false,
                RedirectStandardOutput = true,
                Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}",
                UserId, Password, Server, Database),
                UseShellExecute = false
            };
            Process process = Process.Start(psi);
            #endregion

            #region Recive data
            string output;
            output = process.StandardOutput.ReadToEnd();
            #endregion

            #region Write data to backup file
            StreamWriter file = new StreamWriter(path);
            file.WriteLine(output);
            file.Close();
            #endregion

            #region Close process
            process.WaitForExit();
            process.Close();
            #endregion
        }

        /// <summary>
        /// Restoring SQL db from file.
        /// </summary>
        /// <param name="filePath">Full path to file</param>
        public void Restore(string filePath)
        {
            // Reading backup from file.
            StreamReader file = new StreamReader(filePath);
            string input = file.ReadToEnd();
            file.Close();

            // Prepere process info.
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "mysql",
                RedirectStandardInput = true,
                RedirectStandardOutput = false,
                Arguments = string.Format(@"-u{0} -p{1} -h{2} {3}",
                UserId, Password, Server, Database),
                UseShellExecute = false
            };

            // Starting process that would restore data from backup file.
            Process process = Process.Start(psi);
            // Send data.
            process.StandardInput.WriteLine(input);
            process.StandardInput.Close();
            // Close process.
            process.WaitForExit();
            process.Close();
        }
        #endregion
    }
}

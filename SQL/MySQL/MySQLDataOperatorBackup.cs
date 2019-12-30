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
        /// Backuping database to sql file in directory.
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
    }
}

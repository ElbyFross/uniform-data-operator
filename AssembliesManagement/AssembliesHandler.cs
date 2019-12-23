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
using System.Reflection;
using System.Threading;
using System.IO;

namespace UniformDataOperator.AssembliesManagement
{
    /// <summary>
    /// Handler class that provides an API to managing assemblies.
    /// </summary>
    public static class AssembliesHandler
    {
        /// <summary>
        /// Loads assemblies from a requested path.
        /// </summary>
        /// <param name="path">A folder where will be stored asseblies.</param>
        public static void LoadAsseblies(string path)
        {
            LoadAssemblies(path, SearchOption.AllDirectories, true);
        }

        /// <summary>
        /// Loads assemblies from a requested path.
        /// </summary>
        /// <param name="path">A folder where will be stored asseblies.</param>
        /// <param name="searchOption">Define if a search operation will affect only a root directory or also check child ones.</param>
        /// <param name="spawnDirectory">Should a handler create a directory in case if not existed.</param>
        public static void LoadAssemblies(string path, SearchOption searchOption, bool spawnDirectory)
        {
            // Validate directory.
            bool dirExist = Directory.Exists(path);
            if (!dirExist)
            {
                if (spawnDirectory)
                {
                    Console.WriteLine("Libs directory not found. Creating new one...\n{0}", path);
                    Directory.CreateDirectory(path);
                    Console.WriteLine("");
                }

                // Directory not exist yet so there is nothing to search.
                return;
            }

            // Search files in directory.
            string[] dllFiles = Directory.GetFiles(path, "*.dll", searchOption);

            // Loading assemblies.
            if (dllFiles.Length > 0)
            {
                Console.WriteLine("ASSEMBLIES DETECTED:");
            }
            foreach (string _path in dllFiles)
            {
                try
                {
                    Assembly.LoadFrom(_path);
                    Console.WriteLine(_path.Substring(_path.LastIndexOf("\\") + 1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DLL \"{0}\" LOADING FAILED: {1}",
                        _path.Substring(_path.LastIndexOf("\\") + 1),
                        ex.Message);
                }
            }

            if (dllFiles.Length > 0)
            {
                Console.WriteLine();
            }
        }
    }
}

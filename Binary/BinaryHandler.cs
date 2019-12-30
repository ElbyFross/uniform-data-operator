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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UniformDataOperator.Binary
{
    /// <summary>
    /// Provide API to working with binary files.
    /// </summary>
    public static class BinaryHandler
    {
        static BinaryHandler()
        {
            // Subscribe on assemblies access fail.
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        /// <summary>
        /// Occurs when assebly access is failed.
        /// </summary>
        /// <param name="sender">Object that initiate that event.</param>
        /// <param name="args">Data about target requested assebly.</param>
        /// <returns></returns>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Log.
            //Console.WriteLine("Assembly error: Tying to find assembly `" + args.Name + "` among loaded.");

            #region Fixing .Net bug when requiested assembly not visible via loaded.
            // Getting current available assemblies.
            var assebmliess = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assebmliess)
            {
                // Check if assebly with that name is available.
                if (assembly.FullName.StartsWith(args.Name))
                {
                    // Reloding assembly.
                    var reloaded = Assembly.LoadFrom(assembly.Location);
                    Console.WriteLine("AssemblyResolve: `" + args.Name + "` reloaded.");
                    return reloaded;
                }
            }
            #endregion

            Console.WriteLine("AssemblyResolve: `" + args.Name + "` not found.");
            return null;
        }

        #region Converting
        /// <summary>
        /// Convert object to bytes array.
        /// </summary>
        /// <param name="obj">Object for serialization.</param>
        /// <returns>Binary data</returns>
        public static byte[] ToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Convert bytes array to object.
        /// </summary>
        /// <typeparam name="T">Type of target object.</typeparam>
        /// <param name="data">Binary data.</param>
        /// <returns>Deserialized object.</returns>
        public static T FromByteArray<T>(byte[] data)
        {
            if (data == null)
                return default;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
                ms.Position = 0;
                try
                {
                    object obj = bf.Deserialize(ms);
                    return (T)obj;
                }
                catch(Exception ex)
                {
                    Console.WriteLine("FROM BINARY PARSER ERROR: " + ex.Message);
                    throw ex;
                }
            }
        }
        
        /// <summary>
        /// Convert bytes array to object.
        /// </summary>
        /// <param name="data">Binary data.</param>
        /// <returns>Deserialized object.</returns>
        public static object FromByteArray(byte[] data)
        {
            if (data == null)
                return default;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
                ms.Position = 0;
                try
                {
                    object obj = bf.Deserialize(ms);
                    return obj;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FROM BINARY PARSER ERROR: " + ex.Message + "\n\n STACK TRACE:\n" + ex.StackTrace);
                    throw ex;

                    //Console.WriteLine("Available asseblies.");
                    //var asm = AppDomain.CurrentDomain.GetAssemblies();
                    //foreach(System.Reflection.Assembly asmI in asm)
                    //{
                    //    Console.WriteLine(asmI.FullName);
                    //}

                    //return default;
                }
            }
        }
        #endregion
    }
}

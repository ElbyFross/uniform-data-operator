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
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniformDataOperator.Sql
{
    /// <summary>
    /// Contains base catalog of uniform queries that strongly simplify managing of the data base.
    /// </summary>
    public static class SqlOperatorHandler
    {
        /// <summary>
        /// Event that can be called by operator to share errors during sql commands from async methods.
        /// </summary>
        public static event Action<object, string> SqlErrorOccured;

        /// <summary>
        /// Contains las operator that asing itself to handler as active one.
        /// </summary>
        public static ISqlOperator Active { get; set; }


        /// <summary>
        /// Invoke global error event informing about error occuring.
        /// </summary>
        public static void InvokeSQLErrorOccured(object sender, string message)
        {
            SqlErrorOccured?.Invoke(sender, message);
        }

        /// <summary>
        /// Conver collection view to string order.
        /// 
        /// Elemets of collection must has overrided ToString() methods.
        /// </summary>
        /// <param name="collection">Collection with strings,</param>
        /// <returns>String in format: "i0, i1, ..., in"</returns>
        public static string CollectionToString(IEnumerable<object> collection)
        {
            string result = "";
            foreach (object obj in collection)
            {
                // Skip invalid.
                if (string.IsNullOrEmpty(obj.ToString()))
                {
                    continue;
                }

                result += obj + ", ";
            }

            return result.Length > 2 ? result.Remove(result.Length - 2) : "";
        }

        /// <summary>
        /// Concat to collections in format:
        /// headers0 = [braket]values0[braket], ..., headersN = [braket]valuesN[braket]
        /// 
        /// Elemets of collections must has overrided ToString() methods.
        /// </summary>
        /// <param name="headers">Header of the value.</param>
        /// <param name="values">Value acording to header.</param>
        /// <returns>String that contain collection suitable for SQL commands.</returns>
        public static string ConcatFormatedCollections(
            IEnumerable<object> headers,
            IEnumerable<object> values)
        {
            return ConcatFormatedCollections(headers, values, '\0');
        }

        /// <summary>
        /// Concat to collections in format:
        /// headers0 = [braket]values0[braket], ..., headersN = [braket]valuesN[braket]
        /// 
        /// Elemets of collections must has overrided ToString() methods.
        /// </summary>
        /// <param name="headers">Header of the value.</param>
        /// <param name="values">Value acording to header.</param>
        /// <param name="bracketsSymbol">Symbol that will has been using to clamp value.</param>
        /// <returns>String that contain collection suitable for SQL commands.</returns>
        public static string ConcatFormatedCollections(
            IEnumerable<object> headers,
            IEnumerable<object> values,
            char bracketsSymbol)
        {
            // Validate.
            if (headers.Count() != values.Count())
            {
                throw new InvalidOperationException("Headers and Values collection must contains the same count of elements.");
            }

            string result = "";
            for (int i = 0; i < headers.Count(); i++)
            {
                if (bracketsSymbol != '\0')
                {
                    result += headers.ElementAt(i).ToString() + " = " + bracketsSymbol + values.ElementAt(i).ToString() + bracketsSymbol + ", ";
                }
                else
                {
                    result += headers.ElementAt(i).ToString() + " = " + values.ElementAt(i).ToString() + ", ";
                }
            }

            return result.Length > 2 ? result.Remove(result.Length - 2) : "";
        }

        /// <summary>
        /// Trying to apply data base data to object by members map.
        /// </summary>
        /// <param name="reader">Data base data reader that contains data recived from server.</param>
        /// <param name="members">Map of members that would be lokking in reader.</param>
        /// <param name="obj">Target object that would contain output data.</param>
        /// <param name="error">Error occured during operation. Null if operation is success.</param>
        /// <returns>Result of operation.</returns>
        public static bool DatabaseDataToObject(
            DbDataReader reader,
            IEnumerable<MemberInfo> members, 
            object obj,
            out string error)
        {
            // Drop if data not found.
            if (!reader.Read())
            {
                error = "Data not found.";
                return false;
            }

            // Try to init all maped memvers.
            foreach (MemberInfo member in members)
            {
                Attributes.Column column = member.GetCustomAttribute<Attributes.Column>();

                // Trying to get value from reader relative to this member.
                object receivedValue = null;
                try
                {
                    receivedValue = reader[column.title];
                }
                catch
                {
                    // Skip if data not included to query.
                    continue;
                }

                // If value is empty.
                if (receivedValue is DBNull)
                {
                    // Skip
                    continue;
                }

                try
                {
                    // Try to set value
                    AttributesHandler.SetValue(obj, member, receivedValue);
                }
                catch (Exception ex)
                {
                    // Inform about error during deserialization.
                    error = ex.Message;
                    return false;
                }
            }
            error = null;
            return true;
        }

        /// <summary>
        /// Scaning assemblies and looking for classes and structures with defined Table attribute.
        /// Trying to create shemas and tables via Active SqlOperator.
        /// </summary>
        public static void RescanDatabaseStructure()
        {
            // Load query's processors.
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                // Get all types for assembly.
                foreach (Type type in assembly.GetTypes())
                {
                    // Trying to get table edscriptor.
                    Attributes.Table tableDescriptor = type.GetCustomAttribute<Attributes.Table>();

                    // Check if this type is subclass of query.
                    if (tableDescriptor != null)
                    {
                        // Skip if type was replaced by other.
                        if (Modifiers.TypeReplacer.IsReplaced(type))
                        {
                            Console.WriteLine("SQL Table descriptor was skiped: Type `" + type.FullName + "` was marked as replaced.");
                            continue;
                        }

                        if (!Active.ActivateSchema(tableDescriptor.schema, out string error))
                        {
                            Console.WriteLine("SQL ERROR: Schema creation failed. Details: " + error);
                            continue;
                        }

                        if (!Attributes.Table.TrySetTables(true, out error, type))
                        {
                            Console.WriteLine("SQL ERROR: Table creation failed. Details: " + error);
                            continue;
                        }
                    }
                }
            }
        }
    }
}

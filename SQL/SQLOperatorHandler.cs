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
        /// Contains las operator that asing itself to handler as active one.
        /// </summary>
        public static ISqlOperator Active { get; set; }

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
    }
}

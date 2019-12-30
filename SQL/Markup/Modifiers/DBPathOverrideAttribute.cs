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
using System.Text;
using System.Threading.Tasks;

namespace UniformDataOperator.Sql.Markup.Modifiers
{
    /// <summary>
    /// Overrides database path to member in attributes that looking for this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field |
        AttributeTargets.Property | 
        AttributeTargets.Class |
        AttributeTargets.Struct,
        Inherited = false, 
        AllowMultiple = true)]
    public class DBPathOverrideAttribute : Attribute
    {
        /// <summary>
        /// Type of attribute that would be affected by this overriding.
        /// If null that overriding would be applied to  all who looking for.
        /// </summary>
        public Type targetAttribute;

        /// <summary>
        /// Name of schema that would be used during mentoing of this member in queries if possible.
        /// Will be skiped if null.
        /// </summary>
        public string schema;

        /// <summary>
        /// Name of table that would be used during mentoing of this member in queries if possible.
        /// Will be skiped if null.
        /// </summary>
        public string table;

        /// <summary>
        /// Name of column that would be used during mentoing of this member in queries if possible.
        /// Will be skiped if null.
        /// </summary>
        public string column;

        /// <summary>
        /// Base constructor.
        /// </summary>
        public DBPathOverrideAttribute() { }

        /// <summary>
        /// Constructors that allow to initialize fields via reflected methods.
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="table"></param>
        /// <param name="column"></param>
        /// <param name="targetAttribute"></param>
        public DBPathOverrideAttribute(string schema, string table, string column, Type targetAttribute)
        {
            this.schema = schema;
            this.table = table;
            this.column = column;
            this.targetAttribute = targetAttribute;
        }

        /// <summary>
        /// Looking for path overriding attribute suitable for specified member and assking attribute.
        /// </summary>
        /// <typeparam name="T">Attribute that would be locked as an overriding target</typeparam>
        /// <param name="member">Member that could contains attribute.</param>
        /// <param name="output">Suitable override attribute.</param>
        /// <returns>Result of operation.</returns>
        public static bool TryToGetValidOverride<T>(MemberInfo member, out DBPathOverrideAttribute output) where T : Attribute
        {
            // Get all overriding attributes.
            IEnumerable<Attribute> overriders = member.GetCustomAttributes(typeof(DBPathOverrideAttribute));

            // Check every override attribute.
            foreach(DBPathOverrideAttribute @override in overriders)
            {
                // Check if uniform or for specified type.
                if(@override.targetAttribute == null || @override.Equals(typeof(T)))
                {
                    // Set as output and inform about existing.
                    output = @override;
                    return true;
                }
            }
            // Inform that not exist.
            output = null;
            return false;
        }
    }
}

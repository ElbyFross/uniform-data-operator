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
using MySql.Data.MySqlClient;

namespace UniformDataOperator.Sql.MySql.Markup
{
    /// <summary>
    /// An attribute that can be defined to override some standard `DBType` defined via a `Column` attribute, for columns that willcreated in MySql tables.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class MySqlDBTypeOverrideAttribute : Attribute
    {
        /// <summary>
        /// Type relative to MySql params.
        /// </summary>
        public MySqlDbType type;

        /// <summary>
        /// Type in string format that would be used during column declaration.
        /// Example: VARCHAR(45), BLOB(4196)
        /// </summary>
        public string stringFormat;

        /// <summary>
        /// Overriding standard type's definition of column and specifiy it for MySql databases.
        /// 
        /// Warning: unsafe method, cause just converts type to string to receive stringFormat value. 
        /// Acceptable only for types that hasn't params. 
        /// TINYBLOB as example.
        /// </summary>
        /// <param name="type">Type relative to MySql params.</param>
        public MySqlDBTypeOverrideAttribute(MySqlDbType type)
        {
            this.type = type;
            stringFormat = this.type.ToString();
        }

        /// <summary>
        /// Overriding standard type's definition of column and specifiy it for MySql databases.
        /// </summary>
        /// <param name="type">Type relative to MySql params.</param>
        /// <param name="stringFormat">Type in string format that would be used during column declaration.
        /// Example: VARCHAR(45), BLOB(4196)</param>
        public MySqlDBTypeOverrideAttribute(MySqlDbType type, string stringFormat)
        {
            this.type = type;
            this.stringFormat = stringFormat;
        }
    }
}

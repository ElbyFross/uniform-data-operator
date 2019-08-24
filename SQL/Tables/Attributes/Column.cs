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

namespace UniformDataOperator.SQL.Tables.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Column : Attribute
    {
        /// <summary>
        /// Title of column in table.
        /// </summary>
        public string title;

        /// <summary>
        /// Type of column in table.
        /// </summary>
        public string type;

        /// <summary>
        /// Init column.
        /// </summary>
        /// <param name="title">Title of column in table.</param>
        /// <param name="type">Type of column in table.</param>
        public Column(string title, string type)
        {
            this.title = title;
            this.type = type;
        }

        /// <summary>
        /// Return generated SQL command relative to init time.
        /// </summary>
        public string ColumnDeclarationCommand(MemberInfo member)
        {
            string command = "";

            command += "'" + title + "'";
            command += " " + type;

            if (AttributesHandler.HasAttribute<IsZeroFill>(member))
            {
                command += " ZEROFILL";
            }

            if (AttributesHandler.HasAttribute<IsBinary>(member))
            {
                command += " BINARY";
            }

            if (AttributesHandler.HasAttribute<IsUnsigned>(member))
            {
                command += " UNSIGNED";
            }

            if (AttributesHandler.TryToGetAttribute<Default>(member, out Default hasDefault) && 
                !string.IsNullOrEmpty(hasDefault.defExp))
            {
                // If generated
                if (hasDefault is IsGenerated isGenerated)
                {
                    command += " GENERATED ALWAYS AS(";
                    command += isGenerated.defExp + ") ";
                    command += (isGenerated.mode == IsGenerated.Mode.Stored ? "STORED" : "VIRTUAL");
                }
                // If  has default.
                else
                {
                    command += " DEFAULT " + hasDefault.defExp;
                }
            }

            // If not generated.
            if (hasDefault == null || !(hasDefault is IsGenerated))
            {
                // If has NotNull attribute.
                if (AttributesHandler.HasAttribute<IsNotNull>(member))
                {
                    command += " NOT NULL";
                }
                else
                {
                    command += " NULL";
                }
            }

            // If has AutoIncrement attribute.
            if (AttributesHandler.HasAttribute<IsAutoIncrement>(member))
            {
                command += " AUTO_INCREMENT";
            }

            return command;
        }
    }
}

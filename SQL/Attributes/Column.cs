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
using System.Reflection;
using System.Data;
using System.Data.Common;

namespace UniformDataOperator.Sql.Attributes
{
    /// <summary>
    /// Descriptro of data base table's column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class Column : Attribute
    {
        /// <summary>
        /// Title of column in table.
        /// </summary>
        public string title;

        /// <summary>
        /// Type of column in table.
        /// </summary>
        public DbType type;

        /// <summary>
        /// Init column.
        /// </summary>
        /// <param name="title">Title of column in table.</param>
        /// <param name="type">Type of column in table.</param>
        public Column(string title, DbType type)
        {
            this.title = title;
            this.type = type;
        }

        /// <summary>
        /// Converting column to string view.
        /// </summary>
        /// <param name="column">Input column.</param>
        public static implicit operator string(Column column)
        {
            return column?.title;
        }

        /// <summary>
        /// Converting column to string view.
        /// </summary>
        /// <returns>Tielt of column.</returns>
        public override string ToString()
        {
            return title;
        }

        /// <summary>
        /// Adding members columns data to params.
        /// </summary>
        /// <param name="data">Object that contains values relative to members.</param>
        /// <param name="command">Command objects that would share values.</param>
        /// <param name="members">Members that with defined Column attribute that would be stored to command.</param>
        public static void MembersDataToCommand(ref object data, ref DbCommand command, IEnumerable<MemberInfo> members)
        {
            foreach (MemberInfo member in members)
            {
                // Get column.
                AttributesHandler.TryToGetAttribute<Column>(member, out Column column);

                // Drop generated cirtual columns.
                if (AttributesHandler.TryToGetAttribute<IsGenerated>(member, out IsGenerated isGenerated) &&
                    isGenerated.mode == IsGenerated.Mode.Virual)
                {
                    continue;
                }

                // Add param.
                command.Parameters.Add(
                        SqlOperatorHandler.Active.MemberToParameter(
                            AttributesHandler.GetValue(data, member), 
                        column)
                    );
            }
        }

        /// <summary>
        /// Converting collection of members to lists that contain's splited meta data suitable for queries.
        /// </summary>
        /// <param name="members">Source collection of memers.</param>
        /// <param name="columns">List that contains all detected columns descriptors.</param>
        /// <param name="variables">List that contains names of local variables in format allowed to internal queries generators.</param>
        public static void MembersToMetaLists(IEnumerable<MemberInfo> members,
            out List<Column> columns,
            out List<string> variables)
        {
            // Init lists.
            columns = new List<Column>();
            variables = new List<string>();

            foreach (MemberInfo member in members)
            {
                if(member.GetCustomAttribute<Column>() is Column column)
                {
                    columns.Add(column); // Coping column.
                    variables.Add("@" + column.title); // Generate local var name.
                }
            }
        }
    }
}

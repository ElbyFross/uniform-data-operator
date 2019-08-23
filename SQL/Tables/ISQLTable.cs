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

namespace UniformDataOperator.SQL.Tables
{
    /// <summary>
    /// Interface that allow to get description of target table required for object.
    /// </summary>
    public interface ISQLTable
    {
        /// <summary>
        /// Name of the target schema.
        /// </summary>
        string SchemaName { get; }

        /// <summary>
        /// Name of the table.
        /// </summary>
        string TableName { get; }

        /// <summary>
        /// Engine of this table.
        /// InnoDB by default.
        /// </summary>
        string TableEngine { get; }

        /// <summary>
        /// Array of table fields names.
        /// </summary>
        TableColumnMeta[] TableFields { get; }
    }
}

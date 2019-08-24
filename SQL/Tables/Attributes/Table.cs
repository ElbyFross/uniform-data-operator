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

namespace UniformDataOperator.SQL.Tables.Attributes
{
    /// <summary>
    /// Attribute that would force to automatic generation of table on your SQL server suitable for declered members in class or structure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited=false)]
    public class Table : Attribute
    { 
        /// <summary>
        /// Name of the holding shema.
        /// </summary>
        public string shema;

        /// <summary>
        /// Name of the table.
        /// </summary>
        public string table;

        /// <summary>
        /// Name of the database engine.
        /// </summary>
        public string engine = "InnoDB";

        /// <summary>
        /// Configurate SQL table.
        /// </summary>
        /// <param name="shema">Name of foreign shema.</param>
        /// <param name="table">Name of foreign table.</param>
        public Table(string shema, string table)
        {
            this.shema = shema;
            this.table = table;
        }

        /// <summary>
        /// Configurate SQL table.
        /// </summary>
        /// <param name="shema">Name of foreign shema.</param>
        /// <param name="table">Name of foreign table.</param>
        /// <param name="engine">Name of the database engine.</param>
        public Table(string shema, string table, string engine)
        {
            this.shema = shema;
            this.table = table;
            this.engine = engine;
        }
    }
}

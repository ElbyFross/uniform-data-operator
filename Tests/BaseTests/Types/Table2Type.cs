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
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniformDataOperator.Sql.MySql.Attributes;
using UniformDataOperator.Sql.Tables.Attributes;
using UniformDataOperator.Binary;
using MySql.Data.MySqlClient;

namespace BaseTests.Types
{
    [System.Serializable]
    [Table("testSchema", "testTable2")]
    public class Table2Type
    {
        [Column("fkSourceColumn", DbType.Int32), IsPrimaryKey]
        public int intFK;

        [Column("stringVar", DbType.String)]
        [MySqlDBTypeOverride(MySqlDbType.VarChar, "VARCHAR(100)")] // Overriding to MySql DB with speciving of var size.
        public string stringVar = "testString";
    }
}

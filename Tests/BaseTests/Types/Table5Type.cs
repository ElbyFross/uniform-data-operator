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
using UniformDataOperator.Sql.MySql.Markup;
using UniformDataOperator.Sql.Markup;
using UniformDataOperator.Sql.Markup.Modifiers;
using UniformDataOperator.Binary;
using MySql.Data.MySqlClient;

namespace BaseTests.Types
{
    [Serializable]
    [Table("testSchema2", "testTable5")]
    [DBPathOverride(schema = "abcSchema", targetAttribute = typeof(IsForeignKeyAttribute))]
    public class Table5Type
    {
        [Column("intPKAI", DbType.Int32), IsPrimaryKey, IsAutoIncrement]
        public int intPKAI;
        
        [Column("intFK", DbType.Int32), IsForeignKey("testSchema", "testTable2", "fkSourceColumn")]
        public int fk = 4;

        [Column("stringVar", DbType.String), IsUnique]
        public string stringVar = "testString";
    }
}

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
    [Table("testSchema", "testTable")]
    public class TableType
    {
        [Column("pk", DbType.Int32), IsAutoIncrement, IsPrimaryKey]
        public int pk;

        [Column("intVar", DbType.Int32), Commentary("Test comment")]
        public int intVar;

        [Column("intProp", DbType.Int32), IsUnique]
        public int IntProp
        {
            get
            {
                return _intProp;
            }
            set
            {
                _intProp = value;
            }
        }
        public int _intProp = 123456;

        [Column("uintVar", DbType.Int32)]
        public uint uintVar = 4;

        [Column("intFK", DbType.Int32), IsForeignKey("testSchema", "testTable2", "fkSourceColumn")]
        public int fk = 4;

        //[Column("generatedVirtual", "INT"), IsGenerated(IsGenerated.Mode.Virual, ""]
        //public int generatedCirtual;

        [Column("blobProp", DbType.Binary)]
        [MySqlDBTypeOverride(MySqlDbType.TinyBlob)] // Override type for MySql db.
        public byte[] BlobProp
        {
            get
            {
                return BinaryHandler.ToByteArray<BlobType>(blob);
            }
            set
            {
                if(!(value is byte[]))
                {
                    throw new InvalidCastException("Blob must be shared via byte[].");
                }
                blob = BinaryHandler.FromByteArray<BlobType>(value);
            }
        }
        public BlobType blob;
    }
}

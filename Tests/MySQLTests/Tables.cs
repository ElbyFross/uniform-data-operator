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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UniformDataOperator.Sql.MySql;
using UniformDataOperator.Sql;
using BaseTests.Types;

namespace MySQLTests
{
    [TestClass]
    public class Tables
    {
        /// <summary>
        /// Setting default SQL operator.
        /// </summary>
        public static void SetDefault()
        {
            // Set MySql operator as active.
            SqlOperatorHandler.Active = new MySqlDataOperator()
            {
                UserId = Local.username,
                Password = Local.password
            };
        }

        /// <summary>
        /// Checking connection to database server.
        /// </summary>
        [TestMethod]
        public void Connection()
        {
            SetDefault();

            if(!SqlOperatorHandler.Active.OpenConnection(out string error))
            {
                Assert.Fail("Connection not opened. " + error);
                return;
            }

            if (!SqlOperatorHandler.Active.CloseConnection())
            {
                Assert.Fail("Connection not closed");
                return;
            }
        }

        /// <summary>
        /// Generating the test's tables.
        /// </summary>
        [TestMethod]
        public void Generate()
        {
            // Set default operator.
            SetDefault();

            // Create schema.
            bool chemaResult = SqlOperatorHandler.Active.ActivateSchema("testSchema", out string error);
            if (!chemaResult)
            {
                Assert.Fail("Schema not created. " + error);
                return;
            }

            // Generate tables.
            bool tableResult = UniformDataOperator.Sql.Tables.Attributes.Table.TrySetTables(
                true, out error, typeof(TableType), typeof(Table2Type));
            if (!tableResult)
            {
                Assert.Fail("Table not created. " + error);
                return;
            }

            // Set obejct's data.
            TableType data = new TableType()
            {
                intVar = 99,
                uintVar = 42,
                blob = new BlobType()
                {
                    i = -10000,
                    s = "set test",
                    ui = 9999
                },
                IntProp = -256,
                fk = 0
            };
            bool setResult = SqlOperatorHandler.Active.SetToTable<TableType>(data, out error);
            if (!setResult)
            {
                Assert.Fail("Data set not operated. " + error);
                return;
            }
        }
    }
}

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

using System.Data.Common;

namespace UniformDataOperator.SQL.Tables
{
    /// <summary>
    /// Provide methods  to allow applying data from DB data readers
    /// to this object that was implemented from that interface.
    /// </summary>
    public interface ISQLDataReadCompatible
    {
        /// <summary>
        /// Read data from data base data reader, and apply it to fields.
        /// </summary>
        /// <param name="reader"></param>
        void ReadSQLObject(DbDataReader reader);
    }
}

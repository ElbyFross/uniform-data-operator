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
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

using System.Reflection;
using UniformDataOperator.Sql.Attributes;
using UniformDataOperator.Sql.Attributes.Modifiers;

namespace UniformDataOperator.Sql.MySql
{
    /// <summary>
    /// Operator that provides possibility to operate data on MySQL data base server.
    /// </summary>
    public partial class MySqlDataOperator : ISqlOperator
    {
        #region Sync
        /// <summary>
        /// Trying to set object data to database.
        /// </summary>
        /// <param name="tableType">Type with defined Table attribute. 
        /// Contains columns\properties with defined column attributes. Using as map for collecting data.</param>
        /// <param name="obj">Instance that contains data tha with the same column sttributes as in tableType.</param>
        /// <param name="error">Occurred error. Null if operation passed success.</param>
        /// <param name="select">Array that contains columns' names that would be requested in select block.
        /// If empty then would auto select all columns.</param>
        /// <param name="where">Array that contains columns' names taht wouyld be added to Where block.</param>
        /// <returns>Result of operation.</returns>
        public bool SetToObject(
            Type tableType, 
            object obj, 
            out string error,
            string[] select, 
            params string[] where)
        {
            // Detect object that contains querie's data.
            object internalObj;
            if (obj is IList objList)
            {
                // Set first element of list as target;
                internalObj = objList[0];
            }
            else
            {
                // Set input object as target.
                internalObj = obj;
            }

            // Get coommon list of available members.
            List<MemberInfo> members = MembersAllowedToSet(tableType, internalObj, out error);

            // Generate command.
            DbCommand command = GenerateSetToObjectCommand(
                tableType,
                internalObj,
                Table.FindMembersByColumns(members, where),
                Table.FindMembersByColumns(members, select));

            // Drop if error has been occured.
            if (!string.IsNullOrEmpty(error))
            {
                error = "Commnad generation failed. Details:\n" + error;
                return false;
            }

            #region Execute query
            // Opening connection to DB srver.
            if (!Active.OpenConnection(out error))
            {
                error = "Connection failed. Details:\n" + error;
                return false;
            }
            command.Connection = SqlOperatorHandler.Active.Connection;

            // Await for reader.
            DbDataReader reader = command.ExecuteReader();

            // Drop if DbDataReader is invalid.           
            if (reader == null || reader.IsClosed)
            {
                error = "DbDataReader is null or closed. Operation declined.";
                return false;
            }

            bool result = true;
            // If collection.
            if (obj is IList)
            {
                bool readed;
                ((IList)obj).Clear();

                // Read stream till possible.
                do
                {
                    /// Instiniate obect for receving.
                    object instance = Activator.CreateInstance(tableType);
                    readed = SqlOperatorHandler.DatabaseDataToObject(reader, members, instance, out _);
                    // If readed then add to output.
                    if (readed)
                    {
                        ((IList)obj).Add(instance);
                    }
                }
                while (readed);
            }
            else
            {
                // Try to apply data from reader to object.
                result = SqlOperatorHandler.DatabaseDataToObject(reader, members, obj, out error);
            }

            // Closing connection.
            Active.CloseConnection();
            #endregion

            return result;
        }

        /// <summary>
        /// Trying to set object data to database.
        /// Automaticly build where block with primary keys.
        /// </summary>
        /// <param name="tableType">Type with defined Table attribute. 
        /// Contains columns\properties with defined column attributes. Using as map for collecting data.</param>
        /// <param name="obj">Instance that contains data tha with the same column sttributes as in tableType.</param>
        /// <param name="error">Occurred error. Null if operation passed success.</param>
        /// <param name="select">Array that contains columns' names that would be requested in select block.
        /// If empty then would auto select all columns.</param>
        /// <returns>Result of operation.</returns>
        public bool SetToObject(
            Type tableType, 
            object obj,
            out string error, 
            params string[] select)
        {
            // Collect pk keys as where expression.
            if (!DetectPKToSet(tableType, out error, out string[] where))
            {
                return false;
            }

            // Apply data to object.
            return SetToObject(tableType, obj, out error, select, where);
        }

        /// <summary>
        /// Trying to set object data to database.
        /// Automaticly build where block with primary keys.
        /// Select all available columns.
        /// </summary>
        /// <param name="tableType">Type with defined Table attribute. 
        /// Contains columns\properties with defined column attributes. Using as map for collecting data.</param>
        /// <param name="obj">Instance that contains data tha with the same column sttributes as in tableType.</param>
        /// <param name="error">Occurred error. Null if operation passed success.</param>
        /// <returns>Result of operation.</returns>
        public bool SetToObject(
            Type tableType, 
            object obj, 
            out string error)
        {
            // Collect pk keys as where expression.
            if (!DetectPKToSet(tableType, out error, out string[] where))
            {
                return false;
            }

            string[] select = new string[0];
            return SetToObject(tableType, obj, out error, select, where);
        }
        #endregion

        #region Async
        /// <summary>
        /// Trying to set object data to database.
        /// 
        /// Ocurred error can be recived via subscribtion on SqlOperatorHandler.SqlErrorOccured event;
        /// </summary>
        /// <param name="tableType">Type with defined Table attribute. 
        /// Contains columns\properties with defined column attributes. Using as map for collecting data.</param>
        /// <param name="cancellationToken">Token that will terminate operation if would be required.</param>
        /// <param name="obj">Instance that contains data tha with the same column sttributes as in tableType.</param>
        /// <param name="select">Array that contains columns' names that would be requested in select block.
        /// If empty then would auto select all columns.</param>
        /// <param name="where">Array that contains columns' names taht wouyld be added to Where block.</param>
        /// <returns>Awaitable task.</returns>
        public async Task SetToObjectAsync(
            Type tableType,
            CancellationToken cancellationToken,
            object obj, 
            string[] select,
            params string[] where)
        {
            // Detect object that contains querie's data.
            object internalObj;
            if (obj is IList objList)
            {
                // Set first element of list as target;
                internalObj = objList[0]; 
            }
            else
            {
                // Set input object as target.
                internalObj = obj;
            }

            // Get coommon list of available members.
            List<MemberInfo> members = MembersAllowedToSet(tableType, internalObj, out string error);

            // Generate command.
            DbCommand command = GenerateSetToObjectCommand(
                tableType,
                internalObj,
                Table.FindMembersByColumns(members, where),
                Table.FindMembersByColumns(members, select));

            // Drop if error has been occured.
            if (!string.IsNullOrEmpty(error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(obj, "Commnad generation failed. Details:\n" + error);
                return;
            }

            #region Execute query
            // Opening connection to DB srver.
            if (!Active.OpenConnection(out error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(obj, "Connection failed. Details:\n" + error);
                return;
            }
            command.Connection = SqlOperatorHandler.Active.Connection;

            // Await for reader.
            DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            // Drop if DbDataReader is invalid.           
            if (reader == null || reader.IsClosed)
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(obj,
                    "DbDataReader is null or closed. Operation declined.");
                return;
            }

            // If collection.
            if (obj is IList)
            {
                bool readed;
                ((IList)obj).Clear();

                // Read stream till possible.
                do
                {
                    /// Instiniate obect for receving.
                    object instance = Activator.CreateInstance(tableType);
                    readed = SqlOperatorHandler.DatabaseDataToObject(reader, members, instance, out _);
                    // If readed then add to output.
                    if (readed)
                    {
                        ((IList)obj).Add(instance);
                    }
                }
                while (readed);
            }
            // If single object.
            else
            { 
                // Try to apply data from reader to object.
                if (!SqlOperatorHandler.DatabaseDataToObject(reader, members, obj, out error))
                {
                    // Log error.
                    SqlOperatorHandler.InvokeSQLErrorOccured(obj, error);
                }
            }

            // Closing connection.
            Active.CloseConnection();
            #endregion
        }

        /// <summary>
        /// Trying to set object data to database.
        /// 
        /// Ocurred error can be recived via subscribtion on SqlOperatorHandler.SqlErrorOccured event;
        /// </summary>
        /// <param name="tableType">Type with defined Table attribute. 
        /// Contains columns\properties with defined column attributes. Using as map for collecting data.</param>
        /// <param name="cancellationToken">Token that will terminate operation if would be required.</param>
        /// <param name="obj">Instance that contains data tha with the same column sttributes as in tableType.</param>
        /// <param name="select">Array that contains columns' names that would be requested in select block.
        /// If empty then would auto select all columns.</param>
        /// <returns>Awaitable task.</returns>
        public async Task SetToObjectAsync(
            Type tableType,
            CancellationToken cancellationToken,
            object obj,
            params string[] select)
        {
            // Collect pk keys as where expression.
            if (!DetectPKToSet(tableType, out string error, out string[] where))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(obj, error);
            }

            await SetToObjectAsync(tableType, cancellationToken, obj, select, where);
        }

        /// <summary>
        /// Trying to set object data to database.
        /// 
        /// Ocurred error can be recived via subscribtion on SqlOperatorHandler.SqlErrorOccured event;
        /// </summary>
        /// <param name="tableType">Type with defined Table attribute. 
        /// Contains columns\properties with defined column attributes. Using as map for collecting data.</param>
        /// <param name="cancellationToken">Token that will terminate operation if would be required.</param>
        /// <param name="obj">Instance that contains data tha with the same column sttributes as in tableType.</param>
        /// <returns>Awaitable task.</returns>
        public async Task SetToObjectAsync(
            Type tableType,
            CancellationToken cancellationToken, 
            object obj)
        {
            // Collect pk keys as where expression.
            if (!DetectPKToSet(tableType, out string error, out string[] where))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(obj, error);
            }

            string[] select = new string[0];
            await SetToObjectAsync(tableType, cancellationToken, obj, select, where);
        }
        #endregion

        #region Services
        /// <summary>
        /// Validate entry data for read query.
        /// </summary>
        /// <param name="tableType">Type that has defined Table attribute.</param>
        /// <param name="obj">Entry object/</param>
        /// <param name="error">Possible error. Null if success.</param>
        /// <returns>Result of validation.</returns>
        private static bool ValidateEntryRD(
            Type tableType,
            object obj,
            out string error)
        {
            // Check is SQL operator exist.
            if (Active == null)
            {
                throw new NullReferenceException("Active 'ISQLOperator' not exist. Select it before managing of database.");
            }

            // Drop if not table descriptor.
            if (!AttributesHandler.TryToGetAttribute<Table>(tableType, out Table _))
            {
                error = "Not defined Table attribute for target type.";
                return false;
            }

            // Drop if object not contains data.
            if (obj == null)
            {
                error = "Target object is null and can't be processed. Operation declined.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Detect common collection of members that can be included to query.
        /// </summary>
        /// <param name="tableType">Type that has defined Table attribute.</param>
        /// <param name="obj">Target object that cantains described primary keys, 
        /// that would be used during query generation.</param>
        /// <param name="error">Error faces during operation.</param>
        /// <returns>List with members that valid to using in set queries.</returns>
        private static List<MemberInfo> MembersAllowedToSet(
            Type tableType,
            object obj,
            out string error)
        {
            if (!ValidateEntryRD(tableType, obj, out error))
            {
                return null;
            }

            #region Mapping
            // Get target type map.
            List<MemberInfo> members = AttributesHandler.FindMembersWithAttribute<Column>(tableType).ToList();
            // Trying to detect member with defined isAutoIncrement attribute that has default value.
            MemberInfo autoIncrementMember;
            try
            {
                autoIncrementMember = IsAutoIncrement.GetIgnorable(ref obj, members);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
            // Remove ignorable.
            if (autoIncrementMember != null)
            {
                members.Remove(autoIncrementMember);
            }

            return members;
        }

        /// <summary>
        /// Trying to generate command that would request objects members from server.
        /// </summary>
        /// <param name="tableType">Type that has defined Table attribute.</param>
        /// <param name="obj">Target object that cantains described primary keys, 
        /// that would be used during query generation.</param>
        /// <param name="where">Collection that contains membewrs that need to be included 
        /// to WHERE Sql block of SELECT query.</param>
        /// <param name="select">Collection that contains membewrs that need to be included 
        /// to SELECT Sql block of SELECT query. If empty then would be auto changed to *</param>
        /// <returns>Generated command suitable for current `Active` SQL server.</returns>
        private static DbCommand GenerateSetToObjectCommand(
            Type tableType,
            object obj,
            IEnumerable<MemberInfo> where,
            IEnumerable<MemberInfo> select)
        {
            // Loking for table descriptor.
            if (!Table.TryToGetTableAttribute(tableType, out Table tableDescriptor, out string error))
            {
                SqlOperatorHandler.InvokeSQLErrorOccured(obj, error);
                return null;
            }

            // Looking for primary keys.
            Column.MembersToMetaLists(
                where,
                out List<Column> membersWhereColumns,
                out List<string> membersWhereVars);

            // Looking for not key elements.
            Column.MembersToMetaLists(
                select,
                out List<Column> membersSelectColumns,
                out List<string> _);
            #endregion

            #region Generate SQL query
            // Init query.
            string query = "SELECT ";
            query += (membersSelectColumns.Count == 0 ? "*" : SqlOperatorHandler.CollectionToString(membersSelectColumns)) + "\n";
            query += "FROM " + tableDescriptor.schema + "." + tableDescriptor.table + "\n";
            query += "WHERE " + SqlOperatorHandler.ConcatFormatedCollections(membersWhereColumns, membersWhereVars) + "\n";
            if (obj is IList objList)
            {
                query += ";\n";
                obj = objList[0]; // Override object to first member that contains data.
            }
            else
            {
                query += "LIMIT 1;\n";
            }

            // Sign query to commandd.
            DbCommand command = SqlOperatorHandler.Active.NewCommand(query);

            // Add values as params of command.
            foreach (MemberInfo pk in where)
            {
                command.Parameters.Add(
                    Active.MemberToParameter(
                            AttributesHandler.GetValue(obj, pk),
                            pk.GetCustomAttribute<Column>()
                        )
                    );
            }
            #endregion

            return command;
        }

        /// <summary>
        /// Looking for primary keys in table and build them names to array.
        /// </summary>
        /// <param name="tableType">Type that describe table.</param>
        /// <param name="error">Error if occured. Null if operation success.</param>
        /// <param name="pksArray">Output arrey that contains names of PK columns.</param>
        /// <returns></returns>
        private static bool DetectPKToSet(Type tableType, out string error, out string[] pksArray)
        {
            // Get primary keys desribed in table.
            var pkMembers = AttributesHandler.FindMembersWithAttribute<IsPrimaryKey>(tableType);
            pksArray = new string[pkMembers.Count()];
            for (int i = 0; i < pkMembers.Count(); i++)
            {
                try
                {
                    // Get colum title.
                    pksArray[i] = ((MemberInfo)pkMembers.ElementAt(i)).GetCustomAttribute<Column>().title;
                }
                catch (Exception ex)
                {
                    error = "Column descriptor fail: " + ex.Message;
                    return false;
                }
            }

            error = null;
            return true;
        }
        #endregion
    }
}

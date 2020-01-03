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
using System.Data.Common;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using UniformDataOperator.Sql.Markup;

namespace UniformDataOperator.Sql
{
    /// <summary>
    /// Implement that interface to provide possibility to control your database by unified commands.
    /// </summary>
    public interface ISqlOperator
    {
        #region Properties
        /// <summary>
        /// An ip of a server.
        /// </summary>
        string Server { get; set; }

        /// <summary>
        /// A port for access a binded server.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Name a target database.
        /// </summary>
        string Database { get; set; }

        /// <summary>
        /// A user for connection to a SQL server.
        /// </summary>
        string UserId { get; set; }

        /// <summary>
        /// A password for the user impersonation.
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// A handler that manage connection between the app and a database.
        /// </summary>
        DbConnection Connection { get; }
        #endregion

        #region Service
        /// <summary>
        /// Initializes an operator.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Returns a new clear command suitable for current DB.
        /// </summary>
        DbCommand NewCommand();

        /// <summary>
        /// Returns a new clear command suitable for current DB with included command text.
        /// </summary>
        DbCommand NewCommand(string commandText);

        /// <summary>
        /// Converts a value of a member to a database parameter that can be used in some command.
        /// </summary>
        /// <param name="data">A value of an object that will applied to the parameter.</param>
        /// <param name="column">A column attribute relative to member of data.</param>
        /// <returns>
        /// A parameter that could be used in commands to database.
        /// </returns>
        DbParameter MemberToParameter(object data, ColumnAttribute column);

        /// <summary>
        /// Adds some code that disabling SQL checks during executing a command.
        /// </summary>
        /// <param name="command">
        /// A target command that would be modified during the operation.
        /// </param>
        /// <returns>
        /// The modified comand.
        /// </returns>
        DbCommand DisableSqlChecks(DbCommand command);

        /// <summary>
        /// Adds some code that disabling SQL checks during executing a command.
        /// </summary>
        /// <param name="command">
        /// A target command that will be modified during the operation.
        /// </param>
        /// <returns>
        /// The modified comand.
        /// </returns>
        string DisableSqlChecks(string command);

        /// <summary>
        /// Trying to convert DBType to specified type in string format that suitable to this database.
        /// </summary>
        /// <param name="type">
        /// A common DBType.
        /// </param>
        /// <returns>
        /// A type suitable for an SQL command relative to this type of database. 
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// In case if converting not possible.
        /// </exception>
        string DbTypeToString(DbType type);

        /// <summary>
        /// Validates a database table's column according to member attributes.
        /// </summary>
        /// <param name="tableDescriptor">A table metadata.</param>
        /// <param name="columnMember">
        /// A member with defined <see cref="ColumnAttribute"/> 
        /// attribute that will be compared with instantiated into the database. .
        /// </param>
        /// <returns>A result of validation.</returns>
        bool ValidateTableMember(TableAttribute tableDescriptor, MemberInfo columnMember);
        #endregion

        #region Connection
        /// <summary>
        /// Opens new connection to a SQL server.
        /// </summary>
        /// <param name="error">A faced error. Null if passed success.</param>
        /// <returns>A result of connection.</returns>
        bool OpenConnection(out string error);

        /// <summary>
        /// Closes an opened  connection to a SQL server.
        /// </summary>
        /// <returns>A result of connection closing.</returns>
        bool CloseConnection();
        #endregion

        #region Backups
        /// <summary>
        /// Backup a database to recovery container.
        /// The name will be generated by a timestamp.
        /// </summary>
        /// <param name="directory">
        /// A directory that will contains the backup file.
        /// </param>
        void Backup(string directory);

        /// <summary>
        /// Restores a SQL database from a recovery container.
        /// </summary>
        /// <param name="filePath">A full path to the file.</param>
        void Restore(string filePath);
        #endregion

        #region Base commands
        /// <summary>
        /// Adds a schema if not exist. Sets the schema as current one.
        /// </summary>
        /// <param name="schemaName">A name of the schema that will be used\created.</param>
        /// <param name="error">An error faced during operation.</param>
        /// <returns>A result of the opertion.</returns>
        bool ActivateSchema(string schemaName, out string error);

        /// <summary>
        /// Returns generated SQL command that declares the column into the table.
        /// </summary>
        /// <param name="member">A member with defined attributes that describe a column.</param>
        /// <returns>A SQL command relative to target server.</returns>
        string ColumnDeclarationCommand(MemberInfo member);
        #endregion

        #region Writing to a database
        /// <summary>
        /// Creates and handles a command that send a data from object to a database on a SQL server.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="data">
        /// An object that contains fields/properties which values that will be written to a database.
        /// Affected only fields and properties with defined <see cref="ColumnAttribute"/>.
        /// </param>
        /// <param name="error">
        /// An error faced during the operation.
        /// </param>
        /// <returns>A result of the operation.</returns>
        bool SetToTable(Type tableType, object data, out string error);

        /// <summary>
        /// Creates and handles an asynchronous command that send a data from object to a database on a SQL server.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to terminate the operation.</param>
        /// <param name="data">
        /// An object that contains fields/properties which values that will be written to a database.
        /// Affected only fields and properties with defined <see cref="ColumnAttribute"/>.
        /// </param>
        /// <returns>
        /// A started task.
        /// </returns>
        Task SetToTableAsync(Type tableType, CancellationToken cancellationToken, object data);
        #endregion

        #region Reading from a database
        /// <summary>
        /// Sets a data from a <see cref="DbDataReader"/> to an object by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during a query generation.
        /// </param>
        /// <param name="error">An error faced during the operation.</param>
        /// <param name="select">A list of requested columns that will included to the `SELECT` part of an SQL query.</param>
        /// <param name="where">A list of requested columns that will included to the `WHERE` part of an SQL query.</param>
        /// <returns>A result of the operation.</returns>
        bool SetToObject(Type tableType, object obj, out string error, string[] select, params string[] where);

        /// <summary>
        /// Sets a data from a <see cref="DbDataReader"/> to an object by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// </summary>
        /// <param name="tableType">Type that has defined Table attribute.
        /// Would be used as table descriptor during query building.</param>
        /// <param name="obj">Target object that cantains described primary keys, 
        /// that would be used during query generation.</param>
        /// <param name="error">An error faced during the operation.</param>
        /// <param name="select">A list of requested columns that will included to the `SELECT` part of an SQL query.</param>
        /// <returns>A result of the operation.</returns>
        bool SetToObject(Type tableType, object obj, out string error, params string[] select);

        /// <summary>
        /// Sets a data from a <see cref="DbDataReader"/> to an object by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="obj">Target object that cantains described primary keys, 
        /// that would be used during query generation.</param>
        /// <param name="error">An error faced during the operation.</param>
        /// <returns>A result of the operation.</returns>
        bool SetToObject(Type tableType, object obj, out string error);

        /// <summary>
        /// Sets a data from a <see cref="DbDataReader"/> to an objects list by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// Can receive the objects with count starts at one.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during an SQL query generation.
        /// After operation would contains a recived object.
        /// </param>
        /// <param name="collection">
        /// An output collection that will contains the received objects 
        /// with a same Type as the source `obj` instance.
        /// </param>
        /// <param name="error">An error faced during the operation.</param>
        /// <param name="select">A list of requested columns that will included to the `SELECT` part of an SQL query.</param>
        /// <param name="where">A list of requested columns that will included to the `WHERE` part of an SQL query.</param>
        /// <returns>
        /// A result of operation.
        /// Invokes <see cref="SqlOperatorHandler.SqlErrorOccured"/> in case if data not found.
        /// </returns>
        bool SetToObjects(Type tableType, object obj, out IList collection, out string error, string[] select, params string[] where);

        /// <summary>
        /// Sets a data from a <see cref="DbDataReader"/> to an objects list by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// Can receive the objects with count starts at one.
        /// Looks for the data by primary keys values.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during an SQL query generation.
        /// After operation would contains a recived object.
        /// </param>
        /// <param name="collection">
        /// An output collection that will contains the received objects 
        /// with a same Type as the source `obj` instance.
        /// </param>
        /// <param name="error">An error faced during the operation.</param>
        /// <param name="select">A list of requested columns that will included to the `SELECT` part of an SQL query.</param>
        /// <returns>
        /// A result of operation.
        /// Invokes <see cref="SqlOperatorHandler.SqlErrorOccured"/> in case if data not found.
        /// </returns>
        bool SetToObjects(Type tableType, object obj, out IList collection, out string error, params string[] select);

        /// <summary>
        /// Sets a data from a <see cref="DbDataReader"/> to an objects list by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// Can receive the objects with count starts at one.
        /// Selects all the fields\properties described into the table. Looks for the data by primary keys values.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during an SQL query generation.
        /// After operation would contains a recived object.
        /// </param>
        /// <param name="collection">
        /// An output collection that will contains the received objects 
        /// with a same Type as the source `obj` instance.
        /// </param>
        /// <param name="error">An error faced during the operation.</param>
        /// <returns>
        /// A result of operation.
        /// Invokes <see cref="SqlOperatorHandler.SqlErrorOccured"/> in case if data not found.
        /// </returns>
        bool SetToObjects(Type tableType, object obj, out IList collection, out string error);

        /// <summary>
        /// Asynchronously sets a data from a <see cref="DbDataReader"/> to an object by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during a query generation.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to terminate the operation.</param>
        /// <param name="select">A list of requested columns that will included to the `SELECT` part of an SQL query.</param>
        /// <param name="where">A list of requested columns that will included to the `WHERE` part of an SQL query.</param>
        /// <returns>A started task.</returns>
        Task SetToObjectAsync(Type tableType, CancellationToken cancellationToken, object obj, string[] select, params string[] where);

        /// <summary>
        /// Asynchronously sets a data from a <see cref="DbDataReader"/> to an object by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// Looks for the data by primary keys values.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during a query generation.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to terminate the operation.</param>
        /// <param name="select">A list of requested columns that will included to the `SELECT` part of an SQL query.</param>
        /// <returns>A started task.</returns>
        Task SetToObjectAsync(Type tableType, CancellationToken cancellationToken, object obj, params string[] select);

        /// <summary>
        /// Asynchronously sets a data from a <see cref="DbDataReader"/> to an object by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// Selects all the fields\properties described into the table. Looks for the data by primary keys values.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during a query generation.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to terminate the operation.</param>
        /// <returns>A started task.</returns>
        Task SetToObjectAsync(Type tableType, CancellationToken cancellationToken, object obj);

        /// <summary>
        /// Asynchronously sets a data from a <see cref="DbDataReader"/> to an objects list by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// Can receive the objects with count starts at one.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to terminate the operation.</param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during an SQL query generation.
        /// After operation would contains a recived object.
        /// </param>
        /// <param name="callback">
        /// A delegate that will handle received objects collection.
        /// The collection has the same Type as the source `obj` instance.
        /// </param>
        /// <param name="select">A list of requested columns that will included to the `SELECT` part of an SQL query.</param>
        /// <param name="where">A list of requested columns that will included to the `WHERE` part of an SQL query.</param>
        /// <returns>
        /// A started task.
        /// Invokes <see cref="SqlOperatorHandler.SqlErrorOccured"/> in case if data not found.
        /// </returns>
        Task SetToObjectsAsync(
            Type tableType, 
            CancellationToken cancellationToken, 
            object obj,
            Action<IList> callback, 
            string[] select,
            params string[] where);

        /// <summary>
        /// Asynchronously sets a data from a <see cref="DbDataReader"/> to an objects list by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// Can receive the objects with count starts at one.
        /// Looks for the data by primary keys values.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to terminate the operation.</param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during an SQL query generation.
        /// After operation would contains a recived object.
        /// </param>
        /// <param name="callback">
        /// A delegate that will handle received objects collection.
        /// The collection has the same Type as the source `obj` instance.
        /// </param>
        /// <param name="select">A list of requested columns that will included to the `SELECT` part of an SQL query.</param>
        /// <returns>
        /// A started task.
        /// Invokes <see cref="SqlOperatorHandler.SqlErrorOccured"/> in case if data not found.
        /// </returns>
        Task SetToObjectsAsync(
            Type tableType,
            CancellationToken cancellationToken,
            object obj,
            Action<IList> callback,
            params string[] select);

        /// <summary>
        /// Asynchronously sets a data from a <see cref="DbDataReader"/> to an objects list by using a columns map described at the object's <see cref="Type"/>.
        /// Builds an handles an SQL query for request of columns data relative to primary keys described in the object.
        /// Can receive the objects with count starts at one.
        /// Selects all the fields\properties described into the table. Looks for the data by primary keys values.
        /// </summary>
        /// <param name="tableType">
        /// A type that has defined <see cref="TableAttribute"/>.
        /// Will be used as a table descriptor during the query building.
        /// Defines a behavior by attributes defined to the members.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to terminate the operation.</param>
        /// <param name="obj">
        /// A target object that cantains described primary keys, 
        /// that will be used during an SQL query generation.
        /// After operation would contains a recived object.
        /// </param>
        /// <param name="callback">
        /// A delegate that will handle received objects collection.
        /// The collection has the same Type as the source `obj` instance.
        /// </param>
        /// <returns>
        /// A started task.
        /// Invokes <see cref="SqlOperatorHandler.SqlErrorOccured"/> in case if data not found.
        /// </returns>
        Task SetToObjectsAsync(
            Type tableType,
            CancellationToken cancellationToken,
            object obj,
            Action<IList> callback);
        #endregion
    }
}

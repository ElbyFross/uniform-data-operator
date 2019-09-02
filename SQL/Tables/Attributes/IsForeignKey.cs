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
using System.Reflection;

namespace UniformDataOperator.Sql.Tables.Attributes
{
    /// <summary>
    /// Mark fireld as foreign key to other column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IsForeignKey : Attribute
    {
        public enum Action
        {
            NoAction,
            Cascade,
            Restrict,
            SetNull
        }

        /// <summary>
        /// Name of foreign shema.
        /// </summary>
        public string shema;

        /// <summary>
        /// Name of foreign table.
        /// </summary>
        public string table;

        /// <summary>
        /// Name of foreign column.
        /// </summary>
        public string column;

        /// <summary>
        /// Command that would be applied in case of deleting.
        /// </summary>
        public Action onDeleteCommand = Action.NoAction;

        /// <summary>
        /// Command that would be applied in case of updating.
        /// </summary>
        public Action onUpdateCommand = Action.NoAction;

        /// <summary>
        /// Configurate forgeign column reference.
        /// </summary>
        /// <param name="shema">Name of foreign shema.</param>
        /// <param name="table">Name of foreign table.</param>
        /// <param name="column">Name of foreign column.</param>
        public IsForeignKey(string shema, string table, string column)
        {
            this.shema = shema;
            this.table = table;
            this.column = column;
        }

        /// <summary>
        /// Configurate forgeign column reference.
        /// </summary>
        /// <param name="shema">Name of foreign shema.</param>
        /// <param name="table">Name of foreign table.</param>
        /// <param name="column">Name of foreign column.</param>
        /// <param name="onDeleteCommand">Command that would be applied in case of deleting.</param>
        /// <param name="onUpdateCommand">Command that would be applied in case of updating.</param>
        public IsForeignKey(string shema, string table, string column, Action onDeleteCommand, Action onUpdateCommand)
        {
            this.shema = shema;
            this.table = table;
            this.column = column;

            this.onDeleteCommand = onDeleteCommand;
            this.onUpdateCommand = onUpdateCommand;
        }

        /// <summary>
        /// Convert action to string format.
        /// </summary>
        /// <param name="action">Target action.</param>
        /// <returns>Command format action.</returns>
        public static string ActionToCommand(Action action)
        {
            switch (action)
            {
                case Action.NoAction: return "NO ACTION";
                case Action.Restrict: return "RESTRICT";
                case Action.Cascade: return "CASCADE";
                case Action.SetNull: return "SET NULL";
                default: return "NO ACTION";
            }
        }


        /// <summary>
        /// Return index init string suitable from forgeign key suitable for this column.
        /// </summary>
        /// <param name="selfTableName">Name of the table that contain column.</param>
        /// <returns>Return SQL command that wold generate FK index.</returns>
        public static string FKIndexDeclarationCommand(MemberInfo member, string selfTableName)
        {
            // Looking for column and FK definitions.
            if (!AttributesHandler.TryToGetAttribute<Column>(member, out Column column) || 
                !AttributesHandler.TryToGetAttribute<IsForeignKey>(member, out IsForeignKey isForeignKey)) 
            {
                // Deop if not defined.
                return "";
            }

            return FKIndexDeclarationCommand(column, isForeignKey, selfTableName);
        }

        /// <summary>
        /// Return index init string suitable from forgeign key suitable for this column.
        /// </summary>
        /// <param name="column">Column attribute.</param>
        /// <param name="isForeignKey">FL attribute</param>
        /// <param name="selfTableName">Name of holding table.</param>
        /// <returns>Generated SQL command.</returns>
        public static string FKIndexDeclarationCommand(Column column, IsForeignKey isForeignKey, string selfTableName)
        {
            // Generate comman
            string command = "INDEX `" +
                isForeignKey.FKName(selfTableName) +
                "_idx` (`" +
                column.title +
                "` ASC) VISIBLE";

            return command;
        }

        /// <summary>
        /// Generate fk key name related to this column.
        /// </summary>
        /// <param name="selfTableName">Name of the table that contain column.</param>
        /// <returns>Return FK suitable name</returns>
        public string FKName(string selfTableName)
        {
            return "fk_" + selfTableName + "_" + table + "1";
        }

        /// <summary>
        /// Generate init string from contrains related to this column.
        /// </summary>
        /// <param name="column">Column attribute.</param>
        /// <param name="isForeignKey">FK attribute.</param>
        /// <param name="selfTableName">Name of holding table.</param>
        /// <returns>Generated SQL command.</returns>
        public static string ConstrainDeclarationCommand(Column column, IsForeignKey isForeignKey, string selfTableName)
        {
            string command = "";
            command += "CONSTRAINT `" + isForeignKey.FKName(selfTableName) + "`\n";
            command += "\tFOREIGN KEY(`" + column.title + "`)\n";
            command += "\tREFERENCES `" + isForeignKey.shema + "`.`" + isForeignKey.table + "` (`" + isForeignKey.column + "`)\n";
            command += "\tON DELETE " + IsForeignKey.ActionToCommand(isForeignKey.onDeleteCommand) + "\n";
            command += "\tON UPDATE " + IsForeignKey.ActionToCommand(isForeignKey.onUpdateCommand);
            return command;
        }

        /// <summary>
        /// Generate init string from contrains related to this column.
        /// </summary>
        /// <param name="selfTableName">Name of holding table.</param>
        /// <returns>Generated SQL command.</returns>
        public static string ConstrainDeclarationCommand(MemberInfo member, string selfTableName)
        {
            // Looking for column and FK definitions.
            if (!AttributesHandler.TryToGetAttribute<Column>(member, out Column column) ||
                !AttributesHandler.TryToGetAttribute<IsForeignKey>(member, out IsForeignKey isForeignKey))
            {
                // Deop if not defined.
                return "";
            }
            
            return ConstrainDeclarationCommand(column, isForeignKey, selfTableName);
        }
    }
}
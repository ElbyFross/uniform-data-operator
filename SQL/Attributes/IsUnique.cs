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
using System.Reflection;
using UniformDataOperator.AssembliesManagement;

namespace UniformDataOperator.Sql.Attributes
{
    /// <summary>
    /// Is this value must be unique by this column. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class IsUnique : Attribute
    {
        /// <summary>
        /// Return unique index init string.
        /// </summary>
        /// <returns></returns>
        public string UniqueIndexDeclarationCommand(MemberInfo member)
        {
            // Looking for column attribute.
            if(!AttributesHandler.TryToGetAttribute<Column>(member, out Column column))
            {
                // Drop if not column.
                return "";
            }

            return "UNIQUE INDEX `" + column.title + "_UNIQUE` (`" + column.title + "` ASC) VISIBLE";
        }
    }
}

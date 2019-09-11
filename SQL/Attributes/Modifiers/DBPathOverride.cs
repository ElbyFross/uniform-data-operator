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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniformDataOperator.Sql.Attributes.Modifiers
{
    /// <summary>
    /// Overriding data base path to member in attributes that looking for this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field |
        AttributeTargets.Property | 
        AttributeTargets.Class |
        AttributeTargets.Struct,
        Inherited = false, 
        AllowMultiple = true)]
    public class DBPathOverride : Attribute
    {
        /// <summary>
        /// Type of attribute that would be affected by this overriding.
        /// If null that overriding would be applied to  all who looking for.
        /// </summary>
        public Type targetAttribute;

        /// <summary>
        /// Name of scheme that would be used during mentoing of this member in queries if possible.
        /// Will be skiped if null.
        /// </summary>
        public string scheme;

        /// <summary>
        /// Name of table that would be used during mentoing of this member in queries if possible.
        /// Will be skiped if null.
        /// </summary>
        public string table;

        /// <summary>
        /// Name of column that would be used during mentoing of this member in queries if possible.
        /// Will be skiped if null.
        /// </summary>
        public string column;
    }
}

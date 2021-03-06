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

namespace UniformDataOperator.Sql.Markup
{
    /// <summary>
    /// Mark field as generated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IsGeneratedAttribute : DefaultAttribute
    {
        /// <summary>
        /// Modes of generated columns.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Value computed be expression and stored in database as value.
            /// </summary>
            Stored,
            /// <summary>
            /// Value would be computed as expression during every call.
            /// </summary>
            Virual
        }

        /// <summary>
        /// How to operate with value.
        /// </summary>
        public Mode mode = Mode.Virual;
        
        /// <summary>
        /// Init generated expression.
        /// </summary>
        /// <param name="mode">How to operate with value.</param>
        /// <param name="defExp">Default or Expression value.</param>
        public IsGeneratedAttribute(Mode mode, string defExp) : base(defExp)
        {
            this.mode = mode;
        }
    }
}

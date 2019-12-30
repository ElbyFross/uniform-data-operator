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

namespace UniformDataOperator.Sql.Markup
{
    /// <summary>
    /// Add commentary to SQL table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class CommentaryAttribute : Attribute
    {
        /// <summary>
        /// Commentary to the column.
        /// </summary>
        protected string commentary;

        /// <summary>
        /// Init commentary for column in table.
        /// </summary>
        /// <param name="commentary">Commentary to the column.</param>
        public CommentaryAttribute(string commentary)
        {
            this.commentary = commentary;
        }

        /// <summary>
        /// Return comment in string format.
        /// </summary>
        /// <returns>Comment in string format.</returns>
        public override string ToString()
        {
            return commentary;
        }

        /// <summary>
        /// Return comment in string format.
        /// </summary>
        /// <param name="commentary">Input comment.</param>
        public static implicit operator string(CommentaryAttribute commentary)
        {
            return commentary.commentary;
        }
    }
}

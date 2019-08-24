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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace UniformDataOperator
{
    /// <summary>
    /// Provides API to handle attributes.
    /// </summary>
    public static class AttributesHandler
    {
        /// <summary>
        /// Looking for fields with defined target attribute.
        /// </summary>
        /// <typeparam name="T">Attribute's type.</typeparam>
        /// <param name="source">Type the fould by used as source of fields.</param>
        /// <returns>Collection of found attributes.</returns>
        public static IEnumerable<FieldInfo> FindFieldsWithAttribute<T>(Type source) where T : Attribute
        {
            return source.GetFields().Where(f => Attribute.IsDefined(f, typeof(T)));
        }

        /// <summary>
        /// Looking for fields an properties members with defined target attribute.
        /// </summary>
        /// <typeparam name="T">Attribute's type.</typeparam>
        /// <param name="source">Type the fould by used as source of fields.</param>
        /// <returns>Collection of found attributes.</returns>
        public static IEnumerable<MemberInfo> FindMembersWithAttribute<T>(Type source) where T : Attribute
        {
            return 
                source.GetFields().Where<MemberInfo>(f => Attribute.IsDefined(f, typeof(T))).
                Concat(
                source.GetProperties().Where<MemberInfo>(f => Attribute.IsDefined(f, typeof(T)))
                );
        }

        /// <summary>
        /// Looking for members with defined target attribute.
        /// </summary>
        /// <typeparam name="T">Attribute's type.</typeparam>
        /// <param name="source">Type the fould by used as source of fields.</param>
        /// <returns>Collection of found attributes.</returns>
        public static IEnumerable<MemberInfo> FindMembersWithAttribute<T>(IEnumerable<MemberInfo> source) where T : Attribute
        {
            return source.Where(f => Attribute.IsDefined(f, typeof(T)));
        }

        /// <summary>
        /// Check does member has target attribute.
        /// </summary>
        /// <typeparam name="AttributeType">Type of target attribute.</typeparam>
        /// <param name="member"></param>
        /// <returns></returns>
        public static bool HasAttribute<AttributeType>(MemberInfo member)
            where AttributeType : Attribute
        {
            return member.GetCustomAttribute(typeof(AttributeType)) != null;
        }

        /// <summary>
        /// Trying to detect attribute defined on member.
        /// </summary>
        /// <typeparam name="AttributeType">Type of target attribute.</typeparam>
        /// <param name="member">Member object.</param>
        /// <param name="attribute">Outbut that would contains found attribute.</param>
        /// <returns>Result of the search.</returns>
        public static bool TryToGetAttribute<AttributeType>(
            MemberInfo member, 
            out AttributeType attribute) 
            where AttributeType : Attribute
        {
            // Requiest attribute.
            attribute = (AttributeType)member.GetCustomAttribute(typeof(AttributeType));

            // Detect resutl.
            return attribute != null;
        }

        /// <summary>
        /// Looking for properties with defined target attribute.
        /// </summary>
        /// <typeparam name="T">Attribute's type.</typeparam>
        /// <param name="source">Type the fould by used as source of fields.</param>
        /// <returns>Collection of found attributes.</returns>
        public static IEnumerable<PropertyInfo> FindPropertiesWithAttribute<T>(Type source) where T : Attribute
        {
            return source.GetProperties().Where(f => Attribute.IsDefined(f, typeof(T)));
        }
    }
}

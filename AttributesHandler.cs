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
        /// <param name="source">Type the would by used as source of fields.</param>
        /// <returns>Collection of found attributes.</returns>
        public static IEnumerable<FieldInfo> FindFieldsWithAttribute<T>(Type source) where T : Attribute
        {
            return source.GetFields().Where(f => Attribute.IsDefined(f, typeof(T)));
        }

        /// <summary>
        /// Looking for fields an properties members with defined target attribute.
        /// </summary>
        /// <typeparam name="T">Attribute's type.</typeparam>
        /// <param name="source">Type the would by used as source of fields.</param>
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
        /// <param name="source">Type the would by used as source of fields.</param>
        /// <returns>Collection of found attributes.</returns>
        public static IEnumerable<MemberInfo> FindMembersWithAttribute<T>(IEnumerable<MemberInfo> source) where T : Attribute
        {
            return source.Where(f => Attribute.IsDefined(f, typeof(T)));
        }

        /// <summary>
        /// Looking for members without defined target attribute.
        /// </summary>
        /// <typeparam name="T">Attribute's type.</typeparam>
        /// <param name="source">Type the would by used as source of fields.</param>
        /// <returns>Collection of found attributes.</returns>
        public static IEnumerable<MemberInfo> FindMembersWithoutAttribute<T>(IEnumerable<MemberInfo> source, System.Func<MemberInfo, bool> expression) where T : Attribute
        {
            return source.Where(f => !Attribute.IsDefined(f, typeof(T)) && expression.Invoke(f));
        }
        /// <summary>
        /// Looking for members without defined target attribute.
        /// </summary>
        /// <typeparam name="T">Attribute's type.</typeparam>
        /// <param name="source">Type the would by used as source of fields.</param>
        /// <returns>Collection of found attributes.</returns>
        public static IEnumerable<MemberInfo> FindMembersWithoutAttribute<T>(IEnumerable<MemberInfo> source) where T : Attribute
        {
            return source.Where(f => !Attribute.IsDefined(f, typeof(T)));
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
        /// <param name="source">Type the would by used as source of fields.</param>
        /// <returns>Collection of found attributes.</returns>
        public static IEnumerable<PropertyInfo> FindPropertiesWithAttribute<T>(Type source) where T : Attribute
        {
            return source.GetProperties().Where(f => Attribute.IsDefined(f, typeof(T)));
        }

        /// <summary>
        /// Return value of member.
        /// </summary>
        /// <param name="holder">Object that contain member info.</param>
        /// <param name="member">Target memeber's info.</param>
        /// <returns>Value of member.</returns>
        public static object GetValue(object holder, MemberInfo member)
        {
            if (member is PropertyInfo pi)
            {
                return pi.GetValue(holder);
            }

            if (member is FieldInfo fi)
            {
                return fi.GetValue(holder);
            }

            throw new InvalidCastException("Member must be PropertyInfo or FieldInfo");
        }

        /// <summary>
        /// Setting value to member on specific object.
        /// </summary>
        /// <param name="holder">Object that contain member info.</param>
        /// <param name="member">Target memeber's info.</param>
        /// <param name="data">Data that would be setted up to member.</param>
        /// <returns></returns>
        public static void SetValue(object holder, MemberInfo member, object data)
        {
            if (member is PropertyInfo pi)
            {
                pi.SetValue(holder, Converter(pi.PropertyType, data));
                return;
            }
            
            if (member is FieldInfo fi)
            {
                fi.SetValue(holder, Converter(fi.FieldType, data));
                return;
            }

            throw new InvalidCastException("Member must be PropertyInfo or FieldInfo");
        }

        /// <summary>
        /// Trying to convers object to other type.
        /// </summary>
        /// <param name="targetType">Prefered type of output object.</param>
        /// <param name="data">Soutce object.</param>
        /// <returns>Converted object. The same if converting not possible</returns>
        public static object Converter(Type targetType, object data)
        {
            switch(Type.GetTypeCode(targetType))
            {
                case TypeCode.UInt16:
                    return Convert.ToUInt16(data);
                case TypeCode.UInt32:
                    return Convert.ToUInt32(data);
                case TypeCode.UInt64:
                    return Convert.ToUInt64(data);

                case TypeCode.Int16:
                    return Convert.ToInt16(data);
                case TypeCode.Int32:
                    return Convert.ToInt32(data);
                case TypeCode.Int64:
                    return Convert.ToInt64(data);

                case TypeCode.Single:
                    return Convert.ToSingle(data);
                case TypeCode.Double:
                    return Convert.ToDouble(data);
                case TypeCode.Decimal:
                    return Convert.ToDecimal(data);

                case TypeCode.Boolean:
                    return Convert.ToBoolean(data);

                default: return data;
            }
        }
    }
}

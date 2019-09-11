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
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

using System.Reflection;
using UniformDataOperator.Sql.Attributes;
using UniformDataOperator.Sql.Attributes.Modifiers;

namespace UniformDataOperator.Sql.Attributes
{
    /// <summary>
    /// Is value of this column would incremented relative to previous one during init.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    public class IsAutoIncrement : Attribute
    {
        /// <summary>
        /// Value that would be marked as default for numeric field.
        /// Affectable for primary keys that has IsAutoIncrement definition. 
        /// In case if field\property would contains that alue then interpretator would conclude that value not described and 
        /// would INSERT table's object as new.
        /// In other case object would be described in UPDATE command.
        /// </summary>
        public int ignoreValue = -1;

        /// <summary>
        /// Mark column as auto increament. 
        /// </summary>
        public IsAutoIncrement() { }

        /// <summary>
        /// Mark column as auto increament. Describe value that would be ignored by interpretator fro promary keys.
        /// </summary>
        /// <param name="ignoreValue">Value that would be marked as default for numeric field.
        /// Affectable for primary keys that has IsAutoIncrement definition. 
        /// In case if field\property would contains that alue then interpretator would conclude that value not described and 
        /// would INSERT table's object as new.
        /// In other case object would be described in UPDATE command.</param>
        public IsAutoIncrement(int ignoreValue)
        {
            this.ignoreValue = ignoreValue;
        }

        /// <summary>
        /// Trying to find member with defined IsAutoIncrement attribute in collection.
        /// If found, comparing it's value with ignorable one.
        /// 
        /// Scaning all members that has defined Column attribute.
        /// </summary>
        /// <param name="data">Object that contain member.</param>
        /// <returns>Return member if value equal defined ignorable. 
        /// Null if IsAutoIncrement not defined or object has not defaul value.</returns>
        public static MemberInfo GetIgnorable(ref object data)
        {
            return GetIgnorable(ref data, AttributesHandler.FindMembersWithAttribute<Column>(data.GetType()));
        }

        /// <summary>
        /// Trying to find member with defined IsAutoIncrement attribute in collection.
        /// If found, comparing it's value with ignorable one.
        /// </summary>
        /// <param name="data">Object that contain member.</param>
        /// <param name="members">List of members that would be checked for this object.</param>
        /// <returns>Return member if value equal defined ignorable. 
        /// Null if IsAutoIncrement not defined or object has not defaul value.</returns>
        public static MemberInfo GetIgnorable(ref object data, IEnumerable<MemberInfo> members)
        {
            #region Validate map
            // Detect columns (one in normal) with defined auto increment attribute.
            IEnumerable<MemberInfo> membersAutoInc = AttributesHandler.FindMembersWithAttribute<IsAutoIncrement>(members);
            int count = membersAutoInc.Count();

            if (count > 1)
            {
                string details = "";
                foreach (MemberInfo mi in membersAutoInc)
                {
                    details += "Name: " + mi.Name + " Type: " + mi.DeclaringType.Name + "\n";
                }
                throw new NotSupportedException(
                    "Allowed only one member with defined IsAutoIncrement attribute.\n" +
                    " Cureently defined " + membersAutoInc.Count() + "\nList:\n" + details);
            }
            #endregion

            #region Detect if ignorable
            if (count > 0)
            {
                // Member that has undefined (ignorable) Auto increments value.
                MemberInfo memberInfo = membersAutoInc.First();


                // if contains autoincrement columns.
                // Get auto increment settings descriptor.
                IsAutoIncrement iaiDescriptor = memberInfo.GetCustomAttribute<IsAutoIncrement>();

                object memberValue = AttributesHandler.GetValue(data, memberInfo);
                if(memberInfo is FieldInfo ? 
                    !IsIntLike(((FieldInfo)memberInfo).FieldType) : 
                    !IsIntLike(((PropertyInfo)memberInfo).PropertyType))
                {
                    throw new InvalidCastException("IsAutoIncrement attribute can be applied only to int-like types.\nAllowed types:\n" +
                        "TypeCode.UInt16\n" +
                        "TypeCode.UInt32\n" +
                        "TypeCode.UInt64\n" +
                        "TypeCode.Int16\n" +
                        "TypeCode.Int32\n" +
                        "TypeCode.Int64");
                }
                else
                {
                    return 
                        memberValue.Equals(iaiDescriptor.ignoreValue) ? // If setted default value.
                        memberInfo : // Return ignorable member.
                        null; // Return null cause member must be included to comman.
                }
            }
            #endregion

            // Ignorable not found.
            return null;
        }

        /// <summary>
        /// Cheing does the type is seems like int.
        /// </summary>
        /// <param name="type">Type that would be comparet to ints.</param>
        /// <returns>Result of types commpession.</returns>
        protected static bool IsIntLike(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }
    }
}

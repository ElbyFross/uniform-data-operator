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
using System.Collections;

namespace UniformDataOperator.Modifiers
{
    /// <summary>
    /// Defining of that attribute automaticly add type to
    /// </summary>
    public class TypeReplacer : Attribute
    {
        #region Attribute definition
        /// <summary>
        /// Type that will be added to exlusion table and replaced by using type.
        /// </summary>
        public Type replacingType = null;

        /// <summary>
        /// Type that will be used by everyone who ask exlusion table about.
        /// </summary>
        public Type usingType = null;

        /// <summary>
        /// Allow to use some modifiers. Excluding table will contains only one with hiest priority.
        /// </summary>
        public int overridingPriority = 0;

        /// <summary>
        /// Basse constructor.
        /// </summary>
        public TypeReplacer() { }

        /// <summary>
        /// Constructor that all to initialise attribute's data via reflected constructor.
        /// </summary>
        /// <param name="replacingType">Type that will be added to exlusion table and replaced by using type.</param>
        /// <param name="usingType">Type that will be used by everyone who ask exlusion table about.</param>
        /// <param name="overridingPriority">Allow to use some modifiers. Excluding table will contains only one with hiest priority.</param>
        public TypeReplacer(Type replacingType, Type usingType, int overridingPriority)
        {
            this.replacingType = replacingType;
            this.usingType = usingType;
            this.overridingPriority = overridingPriority;
        }
        #endregion

        #region System managment
        /// <summary>
        /// Table that will contains redefinig instruction for types.
        /// </summary>
        protected static readonly Hashtable ExludingTable = new Hashtable();

        /// <summary>
        /// Meta data building during type replacment registration.
        /// </summary>
        protected class ReplacingMeta
        {
            /// <summary>
            /// Type that must be used instead of requested.
            /// </summary>
            public Type usingType;

            /// <summary>
            /// Priority that was used during type replacement. 
            /// Allow to override that meta in case in next instruction will has highest priority then that one.
            /// </summary>
            public int replacedWithPriority;
        }

        /// <summary>
        /// Returns type that must be eused instead of base type.
        /// </summary>
        /// <param name="baseType">Type that will be checked into excluding table.</param>
        /// <returns>Forwarder type defined into exluding table, or self in case in type not replaced.</returns>
        public static Type GetValidType(Type baseType)
        {
            // Trying to find registred replacing meta.
            if (ExludingTable[baseType] is ReplacingMeta meta)
            {
                return meta.usingType;
            }
            // Meta no found then returing the base type.
            return baseType;
        }

        /// <summary>
        /// Operating type to define if it must be registred in internal systems.
        /// </summary>
        /// <param name="type">Type that could contain defined `TypeReplacing` attribute.</param>
        public static void OperateType(Type type)
        {
            // Try to fine replacing instruction.
            if(!AttributesHandler.TryToGetAttribute<TypeReplacer>(type, out TypeReplacer typeReplacer))
            {
                // Drop cause type has no defined replacing attribute.
                return;
            }

            // Trying to find registred replacing meta.
            if (ExludingTable[typeReplacer.replacingType] is ReplacingMeta meta)
            {
                // Compare by priority. Operate if new instruction has highest priority.
                if(meta.replacedWithPriority < typeReplacer.overridingPriority)
                {
                    // Update replacing data.
                    meta.replacedWithPriority = typeReplacer.overridingPriority;
                    meta.usingType = typeReplacer.usingType;

                    Console.WriteLine("Type `{0}` replaced on `{1}`", typeReplacer.replacingType.FullName, typeReplacer.usingType.FullName);
                }
                return;
            }
            // Type instuction not registred.
            else
            {
                // Registrate new replacing isntruction.
                ExludingTable.Add(
                    typeReplacer.replacingType,
                    new ReplacingMeta()
                    {
                        usingType = typeReplacer.usingType,
                        replacedWithPriority = typeReplacer.overridingPriority
                    });
                
                Console.WriteLine("Type `{0}` replaced on `{1}`", typeReplacer.replacingType.FullName, typeReplacer.usingType.FullName);
            }
        }

        /// <summary>
        /// Checking does that type was replaced.
        /// </summary>
        /// <param name="type">Type that will chached into exluding table.</param>
        /// <returns>Result of check. True if type was replaced.</returns>
        public static bool IsReplaced(Type type)
        {
            // Try to find type's registration in exluded table.
            if(ExludingTable[type] is ReplacingMeta)
            {
                // Type found.
                return true;
            }

            // Type not registred in excluding table.
            return false;
        }

        /// <summary>
        /// Scaning assemblies in current app domaint to find out all types with defined replacing attribute.
        /// </summary>
        public static void RescanAssemblies()
        {
            // Clear current replacing records.
            ExludingTable.Clear();

            // Load query's processors.
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // CHeck every assembly.
            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                var types = assembly.GetTypes();
                // Check every type.
                foreach (Type type in types)
                {
                    // Perform checking operations for that type.
                    OperateType(type);
                }
            }
        }
        #endregion
    }
}

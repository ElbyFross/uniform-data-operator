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

namespace UniformDataOperator.Binary
{
    /// <summary>
    /// Implement algorithm of Boyer-Moore for searching of binary parts.
    /// </summary>
    public static class BoyerMoore
    {

        /// <summary> 
        /// Returns the index within this string of the first occurrence of the
        /// specified substring.If it is not a substring, return -1.
        /// </summary>
        /// <param name="data">Binary array that would be source for search.</param>
        /// <param name="fragment">Binary fragment that would be looked into data array.</param>
        /// <returns>Index of fragment start. -1 if no found.</returns>
        public static int IndexOf(string data, string fragment)
        {
            return IndexOf(Encoding.UTF8.GetBytes(data), Encoding.UTF8.GetBytes(fragment));
        }

        /// <summary> 
        /// Returns the index within this string of the first occurrence of the
        /// specified substring.If it is not a substring, return -1.
        /// </summary>
        /// <param name="data">Binary array that would be source for search.</param>
        /// <param name="fragment">Binary fragment that would be looked into data array.</param>
        /// <returns>Index of fragment start. -1 if no found.</returns>
        public static int IndexOf(byte[] data, byte[] fragment)
        {
            if (fragment.Length == 0)
            {
                return 0;
            }

            int[] charTable = MakeCharTable(fragment);
            int[] offsetTable = MakeOffsetTable(fragment);
            for (int i = fragment.Length - 1; i < data.Length;)
            {
                int j;
                for (j = fragment.Length - 1; fragment[j] == data[i]; --i, --j)
                {
                    if (j == 0)
                    {
                        return i;
                    }
                }

                i += Math.Max(offsetTable[fragment.Length - 1 - j], charTable[data[i]]);
            }

            return -1;
        }

        /// <summary>
        /// Makes the jump table based on the mismatched character information.
        /// </summary>
        /// <param name="fragment">Target binry fragment to search.</param>
        /// <returns>Char table.</returns>
        private static int[] MakeCharTable(byte[] fragment)
        {
            const int ALPHABET_SIZE = 256;
            int[] table = new int[ALPHABET_SIZE];
            for (int i = 0; i < table.Length; ++i)
            {
                table[i] = fragment.Length;
            }

            for (int i = 0; i < fragment.Length - 1; ++i)
            {
                table[fragment[i]] = fragment.Length - 1 - i;
            }

            return table;
        }

        /// <summary>
        /// Makes the jump table based on the scan offset which mismatch occurs.
        /// </summary>
        /// <param name="fragment">Target binry fragment to search.</param>
        /// <returns></returns>
        private static int[] MakeOffsetTable(byte[] fragment)
        {
            int[] table = new int[fragment.Length];
            int lastPrefixPosition = fragment.Length;
            for (int i = fragment.Length - 1; i >= 0; --i)
            {
                if (IsPrefix(fragment, i + 1))
                {
                    lastPrefixPosition = i + 1;
                }

                table[fragment.Length - 1 - i] = lastPrefixPosition - i + fragment.Length - 1;
            }

            for (int i = 0; i < fragment.Length - 1; ++i)
            {
                int slen = SuffixLength(fragment, i);
                table[slen] = fragment.Length - 1 - i + slen;
            }

            return table;
        }

        /// <summary>
        /// Is fragment[p:end] a prefix of fragment?
        /// </summary>
        /// <param name="fragment">Target binry fragment to search.</param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static bool IsPrefix(byte[] fragment, int p)
        {
            for (int i = p, j = 0; i < fragment.Length; ++i, ++j)
            {
                if (fragment[i] != fragment[j])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the maximum length of the substring ends at p and is a suffix.
        /// </summary>
        /// <param name="fragment">Target binry fragment to search.</param>
        /// <param name="p"></param>
        /// <returns></returns>
        private static int SuffixLength(byte[] fragment, int p)
        {
            int len = 0;
            for (int i = p, j = fragment.Length - 1; i >= 0 && fragment[i] == fragment[j]; --i, --j)
            {
                len += 1;
            }

            return len;
        }
    }
}

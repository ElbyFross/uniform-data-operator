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

namespace UniformDataOperator.Binary.IO
{
    /// <summary>
    /// Defines what a type of stream chanel.
    /// </summary>
    public enum StreamChanelMode
    {
        /// <summary>
        /// Server will wait for confirming of data receiving from client.
        /// Has advantage in case of small packages (about 8kb). 
        /// Can take more time for precoessing in case if package is 64kb or higher.
        /// </summary>
        Duplex,
        /// <summary>
        /// Server wait declared count of spins.
        /// </summary>
        Oneway
    }
}

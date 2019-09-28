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
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UniformDataOperator.Binary
{
    /// <summary>
    /// Provide API to working with binary files.
    /// </summary>
    public static class BinaryHandler
    {
        static string log = "";

        /// <summary>
        /// Size of package's header in bytes.
        /// </summary>
        public const int HEADER_SIZE = 4;

        #region Converting
        /// <summary>
        /// Convert object to bytes array.
        /// </summary>
        /// <typeparam name="T">Type of target object.</typeparam>
        /// <param name="obj">Object for serialization.</param>
        /// <returns>Binary data</returns>
        public static byte[] ToByteArray<T>(T obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Convert bytes array to object.
        /// </summary>
        /// <typeparam name="T">Type of target object.</typeparam>
        /// <param name="data">Binary data.</param>
        /// <returns>Deserialized object.</returns>
        public static T FromByteArray<T>(byte[] data)
        {
            if (data == null)
                return default;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
                object obj = bf.Deserialize(ms);
                return (T)obj;
            }
        }
        #endregion

        #region Streaming
        /// <summary>
        /// Writing asynchronicly binary data to stream.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        /// <param name="data">Binary data that would be sent to stream.</param>
        /// <param name="dataBlockSize">Size of block in bytes that would be send to stream per each flush.</param>
        /// <returns>Asynchronous operation of data writing.</returns>
        public static async Task StreamWriterAsync(Stream stream, byte[] data, int dataBlockSize = 8196)
        {
            int iterations = 1600000; 
            await Task.Yield();

            // Open stream writer.
            BinaryWriter sw = new BinaryWriter(stream);

            // Send data size.
            lock (stream)
            {
                // Start writing from 0.
                sw.BaseStream.Position = 0;

                // Get binary data.
                byte[] binaryData = BitConverter.GetBytes(data.Length);
                // Add data to package.
                BuildPackage(0, 0, HEADER_SIZE + 4, binaryData, out byte[] sharedData);

                // Sending data.
                sw.BaseStream.Write(sharedData, 0, sharedData.Length);
                sw.BaseStream.Flush();

                log += "\n Write 1";
            }
            Thread.Yield();
            Thread.SpinWait(iterations);

            // Send used block size
            lock (stream)
            {
                // Start writing from 0.
                sw.BaseStream.Position = 0;

                // Get binary data.
                byte[] binaryData = BitConverter.GetBytes(dataBlockSize);
                // Add data to package.
                BuildPackage(0, 1, HEADER_SIZE + 4, binaryData, out byte[] sharedData);

                // Sending data.
                sw.BaseStream.Write(sharedData, 0, sharedData.Length);
                sw.BaseStream.Flush();

                log += "\n Write 2";
            }
            Thread.Yield();
            Thread.SpinWait(iterations);

            // Compute count of packages.
            int packagesCount = ComputeRequiredPackages(dataBlockSize, data.Length);

            for(int packageIndex = 0; packageIndex < packagesCount; packageIndex++)
            {
                lock (stream)
                {
                    // Start writing from 0.
                    sw.BaseStream.Position = 0;

                    // Building package.
                    BuildPackage(packageIndex, packageIndex + 2, dataBlockSize, data, out byte[] package);

                    // Sending package to stream.
                    sw.BaseStream.Write(package, 0, package.Length);

                    // Release data to underlaying device.
                    sw.BaseStream.Flush();
                }
                Thread.Yield();
                Thread.SpinWait(iterations);

                log += "\n Write 3";
            }
        }

        /// <summary>
        /// Computing count of packages required to data transmission.
        /// </summary>
        /// <param name="blockSize">Size of one transmission package.</param>
        /// <param name="dataBytes">Linght of data to sharing in bytes.</param>
        /// <returns>Count of required packages.</returns>
        public static int ComputeRequiredPackages(int blockSize, int dataBytes)
        {
            float parts = (float)dataBytes / (blockSize - HEADER_SIZE);

            if(parts % 1 != 0.0F)
            {
                return (int)(parts + 1);
            }
            return (int)parts;
        }

        /// <summary>
        /// Building data package by index.
        /// </summary>
        /// <param name="index">Index of data block.</param>
        /// <param name="headerIndex">Index that would be used as header to determine is a data block received.</param>
        /// <param name="blockSize">Size of block including header.</param>
        /// <param name="source">Binary data devided on packages.</param>
        /// <param name="package">Builded binaty package.</param>
        public static void BuildPackage(int index, int headerIndex, int blockSize, byte[] source, out byte[] package)
        {
            int dataBlockSize = blockSize - HEADER_SIZE;
            int leftDataSize = source.Length - dataBlockSize * index;

            // Counputing size of block.
            int count = Math.Min(leftDataSize + HEADER_SIZE, blockSize);

            // Init package array. 
            // 4 bytes for header.
            package = new byte[count];

            // Add package index as header.
            Array.Copy(BitConverter.GetBytes(headerIndex), package, 4);
            
            // Copying data from source to package
            Array.Copy(source, dataBlockSize * index, package, HEADER_SIZE, count - HEADER_SIZE);
        }

        /// <summary>
        /// Asynchronous reading formated data from stream.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        /// <returns>Readed binary data.</returns>
        public static async Task<byte[]> StreamReaderAsync(Stream stream)
        {
            // Open stream reader.
            BinaryReader sw = new BinaryReader(stream);

            // Wait.
            while (sw.BaseStream.Length == 0) Thread.Sleep(5);

#region Receiving data size
            // Receiving block with data size
            int dataSize = 0;
            try
            {
                lock (stream)
                {
                    byte[] dataSizeBinary = new byte[4];
                    sw.BaseStream.Position = HEADER_SIZE;
                    // Reading header that describe data size.
                    sw.Read(dataSizeBinary, 0, 4);
                    dataSize = BitConverter.ToInt32(dataSizeBinary, 0);


                    log += "\n read 1";
                }
            }
            catch (EndOfStreamException efse)
            {
                Console.WriteLine("Stream ended: Operation rejected. Detatils: " + efse.Message);
                return null;
            }
#endregion

#region Receive block size
            // Array that would contains header bytes.
            byte[] header = new byte[HEADER_SIZE];
            
            // Bufer that would contains current block.
            int blockIndex;

            // Wait data flush.
            log += "\n read 2w";
            while (true)
            {
                lock (stream)
                {
                    // Compute current index block.
                    sw.BaseStream.Position = 0;
                    sw.Read(header, 0, HEADER_SIZE);
                }
                blockIndex = BitConverter.ToInt32(header, 0);

                // Drop if second block received.
                if(blockIndex == 1) break;

                // Wait.
                //Thread.Sleep(5);
                await Task.Yield();
            }

            int dataBlockSize = -1;
            try
            {
                lock (stream)
                {
                    byte[] DataBlockSizeBinary = new byte[4];
                    sw.BaseStream.Position = HEADER_SIZE;
                    // Reading header that describe data size.
                    sw.Read(DataBlockSizeBinary, 0, 4);
                    dataBlockSize = BitConverter.ToInt32(DataBlockSizeBinary, 0);

                    log += "\n read 2";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DATA CORUPTED: Data size header not found. Operation rejected. Detatils: " + ex.Message);
                return null;
            }
#endregion
            
#region Receiving data
            // Create data array with target size.
            byte[] data = new byte[dataSize];
            int lastIndex = 2;

            // Reading until finish of declared data.
            for (int dataByteIndex = 0; dataByteIndex < dataSize;)
            {
#region Wait new package
                log += "\n read 3w";

                // Wait data flush.
                while (true)
                {
                    lock (stream)
                    {
                        // Compute current index block.
                        sw.BaseStream.Position = 0;
                        sw.Read(header, 0, HEADER_SIZE);
                    }
                    blockIndex = BitConverter.ToInt32(header, 0);

                    // Drop if second block received.
                    if (blockIndex == lastIndex) break;

                    // Wait.
                    await Task.Yield();
                    //Thread.Sleep(5);
                }
                lastIndex++;
#endregion

                log += "\n read 3";
                lock (stream)
                {
                    sw.BaseStream.Position = HEADER_SIZE;
                    // Fill block.
                    for (int i = HEADER_SIZE; i < dataBlockSize && dataByteIndex < dataSize; i++, dataByteIndex++)
                    {
                        data[dataByteIndex] = sw.ReadByte();
                    }
                }
            }
#endregion

            // Returning received data.
            return data;
        }
#endregion
    }
}

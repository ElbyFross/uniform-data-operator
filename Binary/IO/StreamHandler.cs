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
using System.IO.Pipes;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UniformDataOperator.Binary.IO
{
    /// <summary>
    /// Class that provides way to operating binary data streams.
    /// </summary>
    public static class StreamHandler
    {
        #region Configs
        /// <summary>
        /// Size of package's header in bytes.
        /// </summary>
        public const int HEADER_SIZE = 4;

        /// <summary>
        /// How many spins package builder will wait before manage next package.
        /// </summary>
        public static int OneWaySpinsPause { get; set; } = 1800000;

        /// <summary>
        /// How many miliseconds would wait stream server before drop connection as failed.
        /// </summary>
        public static int MilisecondsBeforeDrop { get; set; } = 5000;
        #endregion

        #region Streaming server
        /// <summary>
        /// Writing asynchronicly binary data to stream.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        /// <param name="mode">
        /// Defines mode of package building. 
        /// In case of Duplex streaming would droped if @MilisecondsBeforeDrop timeout passed without client's confirmation.
        /// </param>
        /// <param name="data">Binary data that would be sent to stream.</param>
        /// <param name="dataBlockSize">Size of block in bytes that would be send to stream per each flush.</param>
        /// <returns>Asynchronous operation of data writing.</returns>
        public static async Task StreamWriterAsync(Stream stream, StreamChanelMode mode, byte[] data, int dataBlockSize = 610000)
        {
            await Task.Yield();

            // Open stream writer.
            BinaryWriter sw = new BinaryWriter(stream);

            // Send data size.
            lock (stream)
            {
                // Start writing from 0.
                sw.BaseStream.Position = 0;

                // Get binary data.
                byte[] binaryData = new byte[12];

                // Set data length to header.
                Array.Copy(BitConverter.GetBytes(data.Length), 0, binaryData, 0, 4);

                // Set data length to header.
                Array.Copy(BitConverter.GetBytes(dataBlockSize), 0, binaryData, 4, 4);

                // Set data length to header.
                Array.Copy(BitConverter.GetBytes((int)mode), 0, binaryData, 8, 4);

                // Add data to package.
                BuildPackage(0, 0, HEADER_SIZE + 12, binaryData, out byte[] sharedData);

                // Sending data.
                sw.BaseStream.Write(sharedData, 0, sharedData.Length);
                sw.BaseStream.Flush();
            }

            // Wait before sending next package.
            WaitPackageReceiving(stream, mode);

            // Compute count of packages.
            int packagesCount = ComputeRequiredPackages(dataBlockSize, data.Length);

            for (int orderIndex = 0; orderIndex < packagesCount; orderIndex++)
            {
                int packageIndex = orderIndex + 1;
                lock (stream)
                {
                    // Start writing from 0.
                    sw.BaseStream.Position = 0;

                    // Building package.
                    BuildPackage(orderIndex, packageIndex, dataBlockSize, data, out byte[] package);

                    // Sending package to stream.
                    sw.BaseStream.Write(package, 0, package.Length);

                    // Release data to underlaying device.
                    sw.BaseStream.Flush();
                }

                // Wait before sending next package.
                WaitPackageReceiving(stream, mode);
            }
        }

        /// <summary>
        /// Writing asynchronicly binary data to stream.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        /// <param name="mode">
        /// Defines mode of package building. 
        /// In case of Duplex streaming would droped if @MilisecondsBeforeDrop timeout passed without client's confirmation.
        /// </param>
        /// <param name="data">Binary data that would be sent to stream.</param>
        /// <param name="dataBlockSize">Size of block in bytes that would be send to stream per each flush.</param>
        /// <returns>Asynchronous operation of data writing.</returns>
        public static async Task StreamWriterAsync(PipeStream stream, StreamChanelMode mode, byte[] data, int dataBlockSize = 128000)
        {
            await Task.Yield();

            // Open stream writer.
            BinaryWriter sw = new BinaryWriter(stream);

            // Send data size.
            lock (stream)
            {
                // Get binary data.
                byte[] binaryData = new byte[12];

                // Set data length to header.
                Array.Copy(BitConverter.GetBytes(data.Length), 0, binaryData, 0, 4);

                // Set data length to header.
                Array.Copy(BitConverter.GetBytes(dataBlockSize), 0, binaryData, 4, 4);

                // Set data length to header.
                Array.Copy(BitConverter.GetBytes((int)mode), 0, binaryData, 8, 4);

                // Add data to package.
                BuildPackage(0, 0, HEADER_SIZE + 12, binaryData, out byte[] sharedData);

                // Sending data.
                sw.BaseStream.Write(sharedData, 0, sharedData.Length);
                sw.BaseStream.Flush();
            }

            // Wait before sending next package.
            WaitPackageReceiving(stream, mode);

            // Compute count of packages.
            int packagesCount = ComputeRequiredPackages(dataBlockSize, data.Length);

            for (int orderIndex = 0; orderIndex < packagesCount; orderIndex++)
            {
                int packageIndex = orderIndex + 1;
                lock (stream)
                {
                    // Start writing from 0.
                    sw.BaseStream.Position = 0;

                    // Building package.
                    BuildPackage(orderIndex, packageIndex, dataBlockSize, data, out byte[] package);

                    // Sending package to stream.
                    sw.BaseStream.Write(package, 0, package.Length);

                    // Release data to underlaying device.
                    sw.BaseStream.Flush();
                }

                // Wait before sending next package.
                WaitPackageReceiving(stream, mode);
            }
        }

        /// <summary>
        /// Whaiting for previous package receiving by client.
        /// </summary>
        /// <param name="stream">Stream to client.</param>
        /// <param name="mode">Mode of stram managing.</param>
        /// <returns></returns>
        public static bool WaitPackageReceiving(Stream stream, StreamChanelMode mode)
        {
            switch (mode)
            {
                case StreamChanelMode.Oneway:
                    Thread.SpinWait(OneWaySpinsPause);
                    break;

                case StreamChanelMode.Duplex:
                    // Open stream reader.
                    BinaryReader sr = new BinaryReader(stream);

                    byte[] header = new byte[HEADER_SIZE]; // Bufer that would contain stream header.
                    int blockIndex; // Buffer that would contain current stream block.

                    #region Wait new package
                    // Wait data flush.
                    while (true)
                    {
                        lock (stream)
                        {
                            // Compute current index block.
                            sr.BaseStream.Position = 0;
                            sr.Read(header, 0, HEADER_SIZE);
                        }
                        blockIndex = BitConverter.ToInt32(header, 0);

                        // Drop if next block request.
                        if (blockIndex == -1)
                        {
                            break;
                        }

                        // Wait.
                        Thread.Sleep(5);
                    }
                    #endregion
                    break;
            }

            return true;
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

            if (parts % 1 != 0.0F)
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
        #endregion

        #region Stream client
        /// <summary>
        /// Asynchronous reading formated data from stream.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        /// <returns>Readed binary data.</returns>
        public static async Task<byte[]> StreamReaderAsync(Stream stream)
        {
            // Open stream reader.
            BinaryReader sr = new BinaryReader(stream);

            // Wait.
            while (sr.BaseStream.Length == 0) Thread.Sleep(5);

            #region Receiving data size
            // Receiving block with data size
            int dataSize = 0;
            int dataBlockSize = -1;
            StreamChanelMode mode;

            try
            {
                lock (stream)
                {
                    byte[] binaryBufer = new byte[4];
                    sr.BaseStream.Position = HEADER_SIZE;

                    // Reading header that describe data size.
                    sr.Read(binaryBufer, 0, 4);
                    dataSize = BitConverter.ToInt32(binaryBufer, 0);

                    // Reading size of data block.
                    sr.Read(binaryBufer, 0, 4);
                    dataBlockSize = BitConverter.ToInt32(binaryBufer, 0);
                    
                    // Reading transmission mode.
                    sr.Read(binaryBufer, 0, 4);
                    mode = (StreamChanelMode)BitConverter.ToInt32(binaryBufer, 0);

                    InformAboutReceving(mode, sr.BaseStream);
                }
            }
            catch (EndOfStreamException efse)
            {
                Console.WriteLine("Stream ended: Operation rejected. Detatils: " + efse.Message);
                return null;
            }
            #endregion

            #region Receiving data
            // Create data array with target size.
            byte[] data = new byte[dataSize];
            int lastIndex = 1;

            // Reading until finish of declared data.
            byte[] header = new byte[HEADER_SIZE]; // Bufer that contains package header.
            int blockIndex;  // Bufer that contains current block index.
            for (int dataByteIndex = 0; dataByteIndex < dataSize;)
            {
                #region Wait new package
                // Wait data flush.
                while (true)
                {
                    lock (stream)
                    {
                        // Compute current index block.
                        sr.BaseStream.Position = 0;
                        sr.Read(header, 0, HEADER_SIZE);
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

                lock (stream)
                {
                    sr.BaseStream.Position = HEADER_SIZE;
                    // Fill block.
                    for (int i = HEADER_SIZE; i < dataBlockSize && dataByteIndex < dataSize; i++, dataByteIndex++)
                    {
                        data[dataByteIndex] = sr.ReadByte();
                    }
                }

                // Inform server that data recevied.
                InformAboutReceving(mode, sr.BaseStream);
            }
            #endregion

            // Returning received data.
            return data;
        }

        public static void InformAboutReceving(StreamChanelMode mode, Stream stream)
        {
            lock (stream)
            {
                // Inform server that data recevied.
                if (mode == StreamChanelMode.Duplex)
                {
                    stream.Position = 0;

                    // Build answer.
                    byte[] binaryData = new byte[4];

                    // Add next index as query.
                    Array.Copy(BitConverter.GetBytes(-1), 0, binaryData, 0, 4);

                    // Send data.
                    stream.Write(binaryData, 0, 4);
                    stream.Flush();
                    Thread.Sleep(5);
                }
            }
        }
        #endregion
    }
}

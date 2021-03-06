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
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BaseTests.Types;
using UniformDataOperator.Binary;
using UniformDataOperator.Binary.IO;

namespace BaseTests.Binary
{
    [TestClass]
    public class BinaryHandlerTests
    {
        [TestMethod]
        public void Serizlize()
        {
            byte[] binary;
            try
            {
                binary  = BinaryHandler.ToByteArray(new BlobType());
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
                return;
            }

            Assert.IsTrue(binary != null && binary.Length > 0);
        }

        [TestMethod]
        public void Deserialize()
        {
            byte[] binary;
            BlobType blob;
            try
            {
                binary = BinaryHandler.ToByteArray(new BlobType() { s = "DeserTest"});
                blob = BinaryHandler.FromByteArray<BlobType>(binary);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
                return;
            }

            Assert.IsTrue(blob != null && blob.s.Equals("DeserTest"));
        }

        [TestMethod]
        public void BoyerMooreSearch()
        {
            string input = "That the string for scan that would be converted to binary format.";
            string fragment = "the string for scan";

            int index = BoyerMoore.IndexOf(input, fragment);

            Assert.IsTrue(index == 5, index.ToString());
        }
        
        [TestMethod]
        public void StreamDataExchange_OneWayStream()
        {
            bool success = false;
            bool completed = false;

            string message = GenerateMessage();

            var stream = new MemoryStream();
            
            var reading = new Task(async delegate ()
            {
                try
                {
                    byte[] data = null;
                    while (data == null)
                    {
                        data = await StreamHandler.StreamReaderAsync(stream);
                        Thread.Sleep(5);
                    }
                    string receivedMessage = BinaryHandler.FromByteArray<string>(data);

                    success = receivedMessage.Equals(message);
                    completed = true;
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.Message);
                    return;
                }
            });
            
            var writing = new Task(async delegate ()
            {
                await StreamHandler.StreamWriterAsync(
                    stream,
                    StreamChanelMode.Oneway,
                    BinaryHandler.ToByteArray(message));
            });


            reading.Start();
            writing.Start();

            while (!completed)
            {
                Thread.Sleep(5);
            }

            stream.Dispose();

            Assert.IsTrue(success);
        }

        [TestMethod]
        public void StreamDataExchange_DuplexStream()
        {
            bool success = false;
            bool completed = false;

            string message = GenerateMessage();

            MemoryStream stream = new MemoryStream();

            var reading = new Task(async delegate ()
            {
                try
                {
                    byte[] data = null;
                    while (data == null)
                    {
                        data = await UniformDataOperator.Binary.IO.StreamHandler.StreamReaderAsync(stream);
                        Thread.Sleep(5);
                    }
                    string receivedMessage = BinaryHandler.FromByteArray<string>(data);

                    success = receivedMessage.Equals(message);
                    completed = true;
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.Message);
                    return;
                }
            });

            var writing = new Task(async delegate ()
            {
                await UniformDataOperator.Binary.IO.StreamHandler.StreamWriterAsync(
                    stream,
                    UniformDataOperator.Binary.IO.StreamChanelMode.Duplex,
                    BinaryHandler.ToByteArray(message));
            });


            reading.Start();
            writing.Start();

            while (!completed)
            {
                Thread.Sleep(5);
            }

            stream.Dispose();

            Assert.IsTrue(success);
        }

        [TestMethod]
        public void StreamDataExchange_Pipes()
        {
            bool success = false;
            bool completed = false;

            int size = 200000000;
            string message = GenerateMessage(size);

            var client = new System.IO.Pipes.NamedPipeClientStream("TESTPIPE");
            var server = new System.IO.Pipes.NamedPipeServerStream("TESTPIPE");

            var banchmarkTimer = new System.Diagnostics.Stopwatch();
            banchmarkTimer.Start();

            var reading = new Task(async delegate ()
            {
                await client.ConnectAsync();

                try
                {
                    // Data to message format.
                    string receivedMessage = await StreamHandler.StreamReaderAsync<string>(client);
                    
                    // Stoping banchmark.
                    banchmarkTimer.Stop();

                    // Validate data.
                    success = receivedMessage.Equals(message);
                    completed = true;
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.Message);
                    return;
                }
            });

            var writing = new Task(async delegate ()
            {
                // Wait client connection.
                await server.WaitForConnectionAsync();

                try
                {
                    // Sending message to stream.
                    await StreamHandler.StreamWriterAsync(server, message);
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.Message);
                    return;
                }
            });


            reading.Start();
            writing.Start();

            while (!completed)
            {
                Thread.Sleep(5);
            }

            float secondsFromStart = banchmarkTimer.ElapsedMilliseconds / (1000.0f);
            float sharedMBSize = size / 1000000.0f;
            float speedMBpS = sharedMBSize / secondsFromStart;
            Console.WriteLine("Transmission time: " + secondsFromStart + " seconds");
            Console.WriteLine("Transmisted: " + sharedMBSize + " MB");
            Console.WriteLine("Speed: " +
                speedMBpS + " MB/s | " + 
                (speedMBpS * 8) + "Mb/s");


            client.Dispose();
            server.Dispose();

            Assert.IsTrue(success);
        }


        /// <summary>
        /// Generating random message with requested length.
        /// </summary>
        /// <param name="lenght">Length of message.</param>
        /// <returns>Generated message.</returns>
        public string GenerateMessage(int lenght = 10000000)
        {
            Random random = new Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, lenght)
                            .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

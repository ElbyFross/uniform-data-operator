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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BaseTests.Types;
using UniformDataOperator.Binary;

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
                binary  = BinaryHandler.ToByteArray<BlobType>(new BlobType());
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
                binary = BinaryHandler.ToByteArray<BlobType>(new BlobType() { s = "DeserTest"});
                blob = BinaryHandler.FromByteArray<BlobType>(binary);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
                return;
            }

            Assert.IsTrue(blob != null && blob.s.Equals("DeserTest"));
        }
    }
}

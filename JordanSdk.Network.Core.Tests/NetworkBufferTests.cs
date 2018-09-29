using Microsoft.VisualStudio.TestTools.UnitTesting;
using JordanSdk.Network.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace JordanSdk.Network.Core.Tests
{
    [TestClass()]
    public class NetworkBufferTests
    {

        static byte[] BIG_BUFFER_DATA;
        static byte[] HUGE_BUFFER_DATA;

        [ClassInitialize]
        public static void ClassInitialized(TestContext context)
        {
            BIG_BUFFER_DATA = new byte[3000];
            Random rnd = new Random();
            rnd.NextBytes(BIG_BUFFER_DATA);
            HUGE_BUFFER_DATA = new byte[Int16.MaxValue * 2];
            rnd.NextBytes(HUGE_BUFFER_DATA);
        }

        [TestMethod(), TestCategory("Buffer (Constructor)")]
        public void NetworkBufferTest1()
        {
            var buffer = new NetworkBuffer(100);
            Assert.AreEqual<int>(0, buffer.Received, "Received obtain a value without data");//An impossible test case
            Assert.AreEqual<int>(100, buffer.Size, "Size was not assign the right value.");//An impossible test case
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Constructor)")]
        public void NetworkBufferTest2()
        {
            var buffer = new NetworkBuffer(100, new byte[] { 0, 1, 2, 3, 4 });
            Assert.AreEqual<int>(5, buffer.Received, "Received was not the right size.");//An impossible test case
            Assert.AreEqual<int>(100, buffer.Size, "Size was not assign the right value.");//An impossible test case
            Assert.IsFalse(buffer.Completed, "Completed should be false.");//An impossible test case
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Constructor)")]
        public void NetworkBufferTest3()
        {
            var buffer = new NetworkBuffer(5, new byte[] { 0, 1, 2, 3, 4 });
            Assert.AreEqual<int>(5, buffer.Received, "Received was not the right size.");//An impossible test case
            Assert.AreEqual<int>(5, buffer.Size, "Size was not assign the right value.");//An impossible test case
            Assert.IsTrue(buffer.Completed, "Completed should be true.");//An impossible test case
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Constructor)")]
        public void NetworkBufferTest4()
        {
            var otherBuffer = new NetworkBuffer(5, new byte[] { 0, 1, 2, 3, 4 });
            var buffer = new NetworkBuffer(otherBuffer);
            Assert.AreEqual<int>(5, buffer.Received, "Received was not the right size.");//An impossible test case
            Assert.AreEqual<int>(5, buffer.Size, "Size was not assign the right value.");//An impossible test case
            Assert.IsTrue(buffer.Completed, "Completed should be true.");//An impossible test case
            buffer.Dispose();
            otherBuffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Append)")]
        public void AppendFillBuffer()
        {
            var buffer = new NetworkBuffer(10);
            buffer.Append(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            Assert.AreEqual<int>(10, buffer.Received, "Received was not the right size.");//An impossible test case
            Assert.AreEqual<int>(10, buffer.Size, "Size was not assign the right value.");//An impossible test case
            Assert.IsTrue(buffer.Completed, "Completed should be true.");//An impossible test case
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Append)")]
        public void AppendAfterCompletedBuffer()
        {
            var buffer = new NetworkBuffer(10);
            buffer.Append(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            try
            {
                buffer.Append(new byte[] { 0 });
                Assert.Fail("Argument out of range exception should be thrown.");
            }
            catch (ArgumentOutOfRangeException)
            {

            }
            catch (Exception)
            {
                Assert.Fail("Nothing other than argument out of range exception should be thrown.");
            }
            Assert.AreEqual<int>(10, buffer.Received, "Received was not the right size.");//An impossible test case
            Assert.AreEqual<int>(10, buffer.Size, "Size was not assign the right value.");//An impossible test case
            Assert.IsTrue(buffer.Completed, "Completed should be true.");//An impossible test case
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Append)")]
        public void AppendBigBufferInChunks()
        {
            var buffer = new NetworkBuffer(BIG_BUFFER_DATA.Length);
            for (int i = 0; i < 3000; i += 100)
            {
                byte[] copy = new byte[100];
                Array.ConstrainedCopy(BIG_BUFFER_DATA, i, copy, 0, 100);
                buffer.Append(copy);
            }
            byte[] data = buffer.GetBuffer();
            Assert.AreEqual<int>(BIG_BUFFER_DATA.Length, buffer.Received, "Received was not the right size.");//An impossible test case
            Assert.AreEqual<int>(BIG_BUFFER_DATA.Length, buffer.Size, "Size was not assign the right value.");//An impossible test case
            Assert.AreEqual<int>(buffer.Received, data.Length, "Received and length should be the same size.");
            Assert.AreEqual<byte>(BIG_BUFFER_DATA[5], data[5], "First byte in buffer should be zero.");
            Assert.AreEqual<byte>(BIG_BUFFER_DATA[1024], data[1024], "Eight byte in buffer should be seven.");
            Assert.IsTrue(buffer.Completed, "Completed should be true.");//An impossible test case
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Append)")]
        public void AppendHugeBuffer()
        {
            var buffer = new NetworkBuffer(HUGE_BUFFER_DATA.Length);
            buffer.Append(HUGE_BUFFER_DATA);
            byte[] data = buffer.GetBuffer();
            Assert.AreEqual<int>(HUGE_BUFFER_DATA.Length, buffer.Received, "Received was not the right size.");//An impossible test case
            Assert.AreEqual<int>(HUGE_BUFFER_DATA.Length, buffer.Size, "Size was not assign the right value.");//An impossible test case
            Assert.AreEqual<int>(buffer.Received, data.Length, "Received and length should be the same size.");
            Assert.AreEqual<byte>(HUGE_BUFFER_DATA[5], data[5], "First byte in buffer should be zero.");
            Assert.AreEqual<byte>(HUGE_BUFFER_DATA[1024], data[1024], "Eight byte in buffer should be seven.");
            Assert.IsTrue(buffer.Completed, "Completed should be true.");//An impossible test case
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Get Buffer)")]
        public void GetBufferAfterOneAppend()
        {
            var buffer = new NetworkBuffer(10);
            buffer.Append(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            byte[] data = buffer.GetBuffer();
            Assert.AreEqual<int>(8, buffer.Received, "Received was not the right size.");//An impossible test case
            Assert.AreEqual<int>(10, buffer.Size, "Size was not assign the right value.");//An impossible test case
            Assert.AreEqual<int>(buffer.Received, data.Length, "Received and length should be the same size.");
            Assert.IsFalse(buffer.Completed, "Completed should be false.");//An impossible test case
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Get Buffer)")]
        public void GetBufferAfterMultipleAppend()
        {
            var buffer = new NetworkBuffer(8);
            buffer.Append(new byte[] { 0, 1, 2, 3 });
            buffer.Append(new byte[] { 4, 5, 6, 7 });
            byte[] data = buffer.GetBuffer();
            Assert.AreEqual<int>(8, buffer.Received, "Received was not the right size.");//An impossible test case
            Assert.AreEqual<int>(8, buffer.Size, "Size was not assign the right value.");//An impossible test case
            Assert.AreEqual<int>(buffer.Received, data.Length, "Received and length should be the same size.");
            Assert.AreEqual<byte>(0, data[0], "First byte in buffer should be zero.");
            Assert.AreEqual<byte>(7, data[7], "Eight byte in buffer should be seven.");
            Assert.IsTrue(buffer.Completed, "Completed should be true.");//An impossible test case
            buffer.Dispose();
        }




        [TestMethod(), TestCategory("Buffer (Append Constrained)")]
        public void AppendConstrainedTest1()
        {
            var buffer = new NetworkBuffer(10);
            var data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            buffer.AppendConstrained(data, 2, 5);
            Assert.AreEqual<int>(5, buffer.Received);
            var received = buffer.GetBuffer();
            Assert.AreEqual<byte>(2, received[0]);
            Assert.AreEqual<byte>(6, received[4]);
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Append Constrained)")]
        public void AppendConstrainedTest2()
        {
            var buffer = new NetworkBuffer(10);
            try
            {
                buffer.AppendConstrained(null, 2, 5);
                Assert.Fail("An exception should be thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentNullException, "The exception thrown should be an \"Argument Null Exception\".");
            }
            finally
            {
                buffer.Dispose();
            }

        }

        [TestMethod(), TestCategory("Buffer (Append Constrained)")]
        public void AppendConstrainedTest3()
        {
            var buffer = new NetworkBuffer(10, new byte[] { 0, 1, 2, 3, 4, 5, 6 });

            try
            {
                buffer.AppendConstrained(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 2, 8);
                Assert.Fail("An exception should be thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentOutOfRangeException, "The exception thrown should be an \"Argument Out Of Range Exception\".");
            }
            finally
            {
                buffer.Dispose();
            }

        }

        [TestMethod(), TestCategory("Buffer (Append Constrained)")]
        public void AppendConstrainedTest4()
        {
            var buffer = new NetworkBuffer(20, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            var data = new byte[] { 10, 121, 55, 32, 44, 56, 31, 46, 71, 88 };
            buffer.AppendConstrained(data, 2, 5);
            Assert.AreEqual<int>(15, buffer.Received);
            var received = buffer.GetBuffer();
            Assert.AreEqual<byte>(0, received[0]);
            Assert.AreEqual<byte>(4, received[4]);
            Assert.AreEqual<byte>(55, received[10]);
            Assert.AreEqual<byte>(31, received[14]);
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Read)")]
        public void ReadTest1()
        {
            var buffer = new NetworkBuffer(20, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            try
            {
                buffer.Read(5);
                Assert.Fail("An exception should have been thrown when attempting to read this buffer.");
            }
            catch (Exception)
            {
                Assert.IsTrue(true);
            }
            finally
            {
                buffer.Dispose();
            }
        }
        [TestMethod(), TestCategory("Buffer (Read)")]
        public void ReadTest2()
        {
            var buffer = new NetworkBuffer(10, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            try
            {
                var bytes = buffer.Read(5);
                Assert.IsNotNull(bytes, "Byte array should not be null.");
                Assert.AreEqual<int>(5, bytes.Length, "Byte array read should contain five elements.");
            }
            catch (Exception ex)
            {
                Assert.Fail($"No exception should be thrown. Exception: ${ex.ToString()}");
            }
            finally
            {
                buffer.Dispose();
            }
        }
        [TestMethod(), TestCategory("Buffer (Read)")]
        public void ReadTest3()
        {
            MemoryStream bytesRead = new MemoryStream();
            var buffer = new NetworkBuffer(BIG_BUFFER_DATA.Length, BIG_BUFFER_DATA);
            for (int i = 0; i < BIG_BUFFER_DATA.Length; i += 1024)
            {
                byte[] read = buffer.Read(1024);
                if(read != null)
                    bytesRead.Write(read, 0, read.Length);
            }
            Assert.AreEqual<long>(bytesRead.Position, BIG_BUFFER_DATA.LongLength, "Bytes written should be equal to bytes read");
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Reset Position)")]
        public void ResetPositionTest1()
        {
            MemoryStream bytesRead = new MemoryStream();
            var buffer = new NetworkBuffer(BIG_BUFFER_DATA.Length);
            buffer.Append(BIG_BUFFER_DATA);
            buffer.ResetPosition();
            try
            {
                byte[] read = buffer.Read(1);
                Assert.AreEqual<byte>(BIG_BUFFER_DATA[0], read[0]);
            }
            catch (Exception)
            {
                Assert.Fail("Reset position should have allowed for the buffer to be readable again.");
            }
            finally
            {
                buffer.Dispose();
            }
        }

        [TestMethod(), TestCategory("Buffer (Reset Position)")]
        public void ResetPositionTest2()
        {
            MemoryStream bytesRead = new MemoryStream();
            var buffer = new NetworkBuffer(BIG_BUFFER_DATA.Length, BIG_BUFFER_DATA);
            byte[] read = buffer.Read(BIG_BUFFER_DATA.Length);
            try
            {
                buffer.Read(1);
                Assert.Fail("An exception should have been thrown when reading a fully read buffer.");
            }catch(Exception)
            {

            }

            buffer.ResetPosition();
            try
            {
                read = buffer.Read(1);
                Assert.AreEqual<byte>(BIG_BUFFER_DATA[0], read[0]);
            }
            catch (Exception)
            {
                Assert.Fail("Reset position should have allowed for the buffer to be readable again.");
            }
            finally
            {
                buffer.Dispose();
            }
        }

        [TestMethod(), TestCategory("Buffer (Resize)")]
        public void ResizeTest1()
        {
            var buffer = new NetworkBuffer(BIG_BUFFER_DATA.Length, BIG_BUFFER_DATA);
            buffer.Resize(1000);
            byte[] read = buffer.GetBuffer();
            Assert.AreEqual<int>(1000, read.Length, "The resized buffer should contain 1000 bytes.");
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Resize)")]
        public void ResizeTest2()
        {
            var buffer = new NetworkBuffer(BIG_BUFFER_DATA.Length, BIG_BUFFER_DATA);
            buffer.Resize(5000);
            byte[] data = buffer.GetBuffer();
            Assert.AreEqual<int>(BIG_BUFFER_DATA.Length, data.Length, "The internal buffer size should not have changed when expanding the buffer.");
            Assert.AreEqual<byte>(BIG_BUFFER_DATA[5], data[5], "Buffer data should be the same after resizing.");
            Assert.AreEqual<byte>(BIG_BUFFER_DATA[1024], data[1024], "Buffer data should be the same after resizing.");
            buffer.Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Clone)")]
        public void CloneTest()
        {
            var buffer = new NetworkBuffer(BIG_BUFFER_DATA.Length, BIG_BUFFER_DATA);
            var copy = buffer.Clone();
            Assert.AreEqual<int>(buffer.Size, copy.Size, "Cloned buffer should have the same size.");
            Assert.AreEqual<int>(buffer.Received, copy.Received, "Cloned buffer should have the same received number of bytes.");
            buffer.Dispose();
            (copy as NetworkBuffer).Dispose();
        }

        [TestMethod(), TestCategory("Buffer (Dispose)")]
        public void DisposeTest()
        {
            var buffer = new NetworkBuffer(BIG_BUFFER_DATA.Length, BIG_BUFFER_DATA);
            buffer.Dispose();
            try
            {
                buffer.Read(1);
                Assert.Fail("Any buffer operation after the buffer is disposed should result in an object disposed exception.");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ObjectDisposedException, "Any buffer operation after the buffer is disposed should result in an object disposed exception. (Error was not Object Disposed Exception)");
            }
        }

      
    }
}
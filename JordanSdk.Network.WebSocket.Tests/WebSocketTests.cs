using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using JordanSdk.Network.Core;
using System.Net;

namespace JordanSdk.Network.WebSocket.Tests
{
    [TestClass()]
    public class TCPSocketTests
    {
        #region Fields
        static byte[] BIG_BUFFER_DATA;
        static byte[] HUGE_BUFFER_DATA;

        static WebSocketProtocol ipv4Server;
        static ISocket ipv4Client;

        static ISocket ipv4ServerClient;

        static string hostAddress = "http://localhost/server/";
        static string serverAddress = "ws://localhost/server";

        const int PORT = 4884;
        #endregion

        #region Test / Class Initialization
        [TestInitialize]
        public void Initialize()
        {

            ManualResetEvent mevent = new ManualResetEvent(false);
            ipv4Server = new WebSocketProtocol() { Port = PORT, Address = hostAddress };
            ipv4Server.Port = PORT;
            ipv4Server.OnConnectionRequested += (socket) =>
            {
                ipv4ServerClient = socket;
                mevent.Set();
            };
            ipv4Server.Listen();
            mevent.Reset();
           
            ipv4Client = this.CreateWSClientProtocol().Connect(serverAddress,PORT);
            mevent.WaitOne(1000);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ipv4Client.Disconnect();
            ipv4Server.StopListening();
            ipv4Server.Dispose();
        }

        [ClassInitialize]
        public static void ClassInitialized(TestContext context)
        {

            BIG_BUFFER_DATA = new byte[3000];
            Random rnd = new Random();
            rnd.NextBytes(BIG_BUFFER_DATA);
            HUGE_BUFFER_DATA = new byte[Int16.MaxValue * 2];
            rnd.NextBytes(HUGE_BUFFER_DATA);
            
        }

        #endregion

        #region Test Cases

        [TestMethod(), TestCategory("Web Sockets (Send)")]
        public void SynchronousIPV4SendSmallBufferTest()
        {
            try
            {
                NetworkBuffer buffer = TestData.GetDummyStream();
                int sent = ipv4Client.Send(buffer.ToArray());
                Assert.AreEqual(buffer.Size, sent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("Web Sockets (Send)")]
        public void SynchronousIPV4SendMidSizeBufferTest()
        {
            try
            {
               
                NetworkBuffer buffer = TestData.GetLargeBuffer();

                int sent = 0;
                byte[] sentData = null;
                while (null != (sentData = buffer.Read(WebSocketProtocol.BUFFER_SIZE)))
                {
                    sent += ipv4Client.Send(sentData);
                    var task = ipv4ServerClient.ReceiveAsync();
                }
                Assert.AreEqual(buffer.Size, sent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("Web Sockets (Send)")]
        public void SynchronousIPV4SendLargeBufferTest()
        {
            try
            {
              
                NetworkBuffer buffer = TestData.GetMidSizeBuffer();
                int sent = 0;
                byte[] sentData = null;
                while (null != (sentData = buffer.Read(WebSocketProtocol.BUFFER_SIZE)))
                {
                    sent += ipv4Client.Send(sentData);
                    var task = ipv4ServerClient.ReceiveAsync();
                }
                Assert.AreEqual(buffer.Size, sent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }


        [TestMethod(), TestCategory("Web Sockets (Send)")]
        public void AsyncCallbackIPV4SendSmallBufferTest()
        {
            try
            {
                ManualResetEventSlim mevent = new ManualResetEventSlim(false);
                NetworkBuffer buffer = TestData.GetDummyStream();
                int bytesSent = 0;

                ipv4Client.SendAsync(buffer.ToArray(), (sent) =>
                {
                    bytesSent += sent;
                    var task = ipv4ServerClient.ReceiveAsync();
                    mevent.Set();

                });
                mevent.Wait(1000);
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("Web Sockets (Send)")]
        public void AsyncCallbackIPV4SendMidSizeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = TestData.GetLargeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                ManualResetEventSlim mevent = new ManualResetEventSlim(false);
                while (null != (sentData = buffer.Read(WebSocketProtocol.BUFFER_SIZE)))
                {
                    mevent.Reset();
                    ipv4Client.SendAsync(sentData, (sent) =>
                    {
                        bytesSent += sent;
                        var task = ipv4ServerClient.ReceiveAsync();
                        mevent.Set();

                    });
                    mevent.Wait(1000);
                }
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("Web Sockets (Send)")]
        public void AsyncCallbackIPV4SendLargeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = TestData.GetMidSizeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                ManualResetEventSlim mevent = new ManualResetEventSlim(false);

                while ((sentData = buffer.Read(WebSocketProtocol.BUFFER_SIZE)) != null)
                {
                    mevent.Reset();
                    ipv4Client.SendAsync(sentData, (sent) =>
                    {
                        bytesSent += sent;
                        ipv4ServerClient.ReceiveAsync();
                        mevent.Set();

                    });
                    mevent.Wait(1000);
                }
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }


        [TestMethod(), TestCategory("Web Sockets (Send)")]
        public async Task AsyncTaskIPV4SendSmallBufferTest()
        {
            try
            {

                NetworkBuffer buffer = TestData.GetDummyStream();
                int bytesSent = await ipv4Client.SendAsync(buffer.ToArray());
                var task = ipv4ServerClient.ReceiveAsync();
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("Web Sockets (Send)")]
        public async Task AsyncTaskIPV4SendMidSizeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = TestData.GetLargeBuffer();

                byte[] sentData = null;
                int bytesSent = 0;
                while (null != (sentData = buffer.Read(WebSocketProtocol.BUFFER_SIZE)))
                {
                    bytesSent += await ipv4Client.SendAsync(sentData);
                    var task = ipv4ServerClient.ReceiveAsync();
                }
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("Web Sockets (Send)")]
        public async Task AsyncTaskIPV4SendLargeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = TestData.GetMidSizeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                while (null != (sentData = buffer.Read(WebSocketProtocol.BUFFER_SIZE)))
                {
                    bytesSent += await ipv4Client.SendAsync(sentData);
                    var task = ipv4ServerClient.ReceiveAsync();
                }
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("Web Sockets (Disconnect)")]
        public void DisconnectTest()
        {
            var socket = this.CreateWSClientProtocol().Connect(serverAddress,PORT);
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            socket.Disconnect();
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("Web Sockets (Disconnect)")]
        public async Task AsyncTaskDisconnectTest()
        {
            var socket = this.CreateWSClientProtocol().Connect(serverAddress,PORT);
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            await socket.DisconnectAsync();
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("Web Sockets (Disconnect)")]
        public void AsyncCallbackDisconnectTest()
        {
            ManualResetEventSlim mevent = new ManualResetEventSlim(false);
            var socket = this.CreateWSClientProtocol().Connect(serverAddress,PORT);
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            socket.DisconnectAsync(()=>
            {
                mevent.Set();
            });
            mevent.Wait(1000);
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("Web Sockets (Receive)")]
        public void ReceiveTest()
        {

            Task.Run(async () => { await Task.Delay(20); ipv4ServerClient.Send(TestData.GetDummyStream().ToArray()); });
            byte[] buffer = ipv4Client.Receive();
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Length > 0);
        }

        [TestMethod(), TestCategory("Web Sockets (Receive)")]
        public async Task AsyncTaskReceiveTest()
        {
            var sentAsync = Task.Run(async () => { await Task.Delay(20); ipv4ServerClient.Send(TestData.GetDummyStream().ToArray()); });
            byte[] buffer = await ipv4Client.ReceiveAsync();
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Length > 0);
        }

        [TestMethod(), TestCategory("Web Sockets (Receive)")]
        public void AsyncCallbackReceiveTest()
        {
            var sentAsync = Task.Run(async () => { await Task.Delay(20); ipv4ServerClient.Send(TestData.GetDummyStream().ToArray()); });
            byte[] received = null;
            ManualResetEventSlim mevent = new ManualResetEventSlim(false);
            ipv4Client.ReceiveAsync((buffer)=>
            {
                received = buffer;
                mevent.Set();
            });
            mevent.Wait(1000);
            Assert.IsNotNull(received);
            Assert.IsTrue(received.Length > 0);
        }


        #endregion
    }
}
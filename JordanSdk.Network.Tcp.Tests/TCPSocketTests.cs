using Microsoft.VisualStudio.TestTools.UnitTesting;
using JordanSdk.Network.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using JordanSdk.Network.Core;

namespace JordanSdk.Network.Tcp.Tests
{
    [TestClass()]
    public class TCPSocketTests
    {
        #region Fields
        static byte[] BIG_BUFFER_DATA;
        static byte[] HUGE_BUFFER_DATA;

        System.Threading.ManualResetEvent mevent;
        static TcpProtocol ipv4Server;
        static TcpProtocol ipv6Server;
        static TcpSocket ipv4Client;
        static TcpSocket ipv6Client;

        #endregion

        #region Test / Class Initialization
        [TestInitialize]
        public void Initialize()
        {
           
            mevent = new System.Threading.ManualResetEvent(false);
            ipv4Client = this.CreateIPV4ClientProtocol().Connect();
            ipv6Client = this.CreateIPV6ClientProtocol().Connect();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ipv4Client.Disconnect();
            ipv6Client.Disconnect();
        }

        [ClassInitialize]
        public static void ClassInitialized(TestContext context)
        {
            ipv4Server = new TcpProtocol();
            ipv4Server.Port = 4884;
            ipv4Server.IPAddressKind = IPAddressKind.IPV4;
            ipv4Server.Listen();
            ipv6Server = new TcpProtocol();
            ipv6Server.Port = 4884;
            ipv6Server.IPAddressKind = IPAddressKind.IPV6;
            ipv6Server.Listen();

            BIG_BUFFER_DATA = new byte[3000];
            Random rnd = new Random();
            rnd.NextBytes(BIG_BUFFER_DATA);
            HUGE_BUFFER_DATA = new byte[Int16.MaxValue * 2];
            rnd.NextBytes(HUGE_BUFFER_DATA);

            
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            ipv4Server.StopListening();
            ipv4Server.Dispose();
            ipv6Server.StopListening();
            ipv6Server.Dispose();
        }

        #endregion

        #region Test Cases

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public void SynchronousIPV4SendSmallBufferTest()
        {
            try
            {
                NetworkBuffer buffer = GetDummyStream();
                int sent = ipv4Client.Send(buffer);
                Assert.AreEqual(buffer.Size, sent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public void SynchronousIPV4SendMidSizeBufferTest()
        {
            try
            {
               
                NetworkBuffer buffer = GetBigStream();
                int sent = ipv4Client.Send(buffer);
                Assert.AreEqual(buffer.Size, sent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public void SynchronousIPV4SendLargeBufferTest()
        {
            try
            {
              
                NetworkBuffer buffer = GetHugeStream();
                int sent = ipv4Client.Send(buffer);
                Assert.AreEqual(buffer.Size, sent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }


        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public void AsyncCallbackIPV4SendSmallBufferTest()
        {
            try
            {
               
                NetworkBuffer buffer = GetDummyStream();
                mevent.Reset();
                int bytesSent = 0;
                ipv4Client.SendAsync(buffer,(sent) =>
                {
                    bytesSent = sent;
                    mevent.Set();

                });
                mevent.WaitOne();
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public void AsyncCallbackIPV4SendMidSizeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = GetBigStream();
                mevent.Reset();
                int bytesSent = 0;
                ipv4Client.SendAsync(buffer, (sent) =>
                {
                    bytesSent = sent;
                    mevent.Set();

                });
                mevent.WaitOne();
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public void AsyncCallbackIPV4SendLargeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = GetHugeStream();
                mevent.Reset();
                int bytesSent = 0;
                ipv4Client.SendAsync(buffer, (sent) =>
                {
                    bytesSent = sent;
                    mevent.Set();

                });
                mevent.WaitOne();
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }


        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public async Task AsyncTaskIPV4SendSmallBufferTest()
        {
            try
            {

                NetworkBuffer buffer = GetDummyStream();
                int bytesSent = await ipv4Client.SendAsync(buffer);
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public async Task AsyncTaskIPV4SendMidSizeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = GetBigStream();
                int bytesSent = await ipv4Client.SendAsync(buffer);
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public async Task AsyncTaskIPV4SendLargeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = GetHugeStream();
                int bytesSent = await ipv4Client.SendAsync(buffer);
                Assert.AreEqual(buffer.Size, bytesSent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPSocket (Disconnect)")]
        public void DisconnectTest()
        {
            var socket = this.CreateIPV4ClientProtocol().Connect();
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            socket.Disconnect();
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("TCPSocket (Disconnect)")]
        public async Task AsyncTaskDisconnectTest()
        {
            var socket = this.CreateIPV4ClientProtocol().Connect();
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            await socket.DisconnectAsync();
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("TCPSocket (Disconnect)")]
        public void AsyncCallbackDisconnectTest()
        {
            mevent.Reset();
            var socket = this.CreateIPV4ClientProtocol().Connect();
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            socket.DisconnectAsync(()=>
            {
                mevent.Set();
            });
            mevent.WaitOne();
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("TCPSocket (Receive)")]
        public void ReceiveTest()
        {
            ISocket serverInstance = null;
            mevent.Reset();
            ipv4Server.OnConnectionRequested += (isocket) =>
            {
                serverInstance = isocket;
                mevent.Set();
            };
            TcpSocket clientInstance = this.CreateIPV4ClientProtocol().Connect();
            
            mevent.WaitOne();
            Assert.AreEqual<string>(clientInstance.Token, serverInstance.Token, "Both instances had different token issued.");
            mevent.Reset();
            Task.Run(async () => { await Task.Delay(20); serverInstance.Send(GetDummyStream()); });
            INetworkBuffer buffer = clientInstance.Receive();
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Size > 0);
        }

        [TestMethod(), TestCategory("TCPSocket (Receive)")]
        public async Task AsyncTaskReceiveTest()
        {
            ISocket serverInstance = null;
            mevent.Reset();
            ipv4Server.OnConnectionRequested += (isocket) =>
            {
                serverInstance = isocket;
                mevent.Set();
            };
            TcpSocket clientInstance = this.CreateIPV4ClientProtocol().Connect();
            mevent.WaitOne();
            Assert.AreEqual<string>(clientInstance.Token, serverInstance.Token, "Both instances had different token issued.");
            var sentAsync = Task.Run(async () => { await Task.Delay(20); serverInstance.Send(GetDummyStream()); });
            INetworkBuffer buffer = await clientInstance.ReceiveAsync();
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Size > 0);
        }

        [TestMethod(), TestCategory("TCPSocket (Receive)")]
        public void AsyncCallbackReceiveTest()
        {
            ISocket serverInstance = null;
            mevent.Reset();
            ipv4Server.OnConnectionRequested += (isocket) =>
            {
                serverInstance = isocket;
                mevent.Set();
            };
            TcpSocket clientInstance = this.CreateIPV4ClientProtocol().Connect();
            mevent.WaitOne();
            Assert.AreEqual<string>(clientInstance.Token, serverInstance.Token, "Both instances had different token issued.");
            var sentAsync = Task.Run(async () => { await Task.Delay(20); serverInstance.Send(GetDummyStream()); });
            INetworkBuffer received = null;
            mevent.Reset();
            clientInstance.ReceiveAsync((buffer)=>
            {
                received = buffer;
                mevent.Set();
            });
            mevent.WaitOne();
            Assert.IsNotNull(received);
            Assert.IsTrue(received.Size > 0);
        }


        #endregion

        #region Helper Tools
        private static NetworkBuffer GetDummyStream()
        {
            byte[] helloWorld = System.Text.Encoding.UTF8.GetBytes("Hello World.");
            return new NetworkBuffer(helloWorld.Length, helloWorld);
        }

        private static NetworkBuffer GetHugeStream() => new NetworkBuffer(HUGE_BUFFER_DATA.Length, HUGE_BUFFER_DATA);


        private static NetworkBuffer GetBigStream() => new NetworkBuffer(BIG_BUFFER_DATA.Length, BIG_BUFFER_DATA);

        #endregion
    }
}
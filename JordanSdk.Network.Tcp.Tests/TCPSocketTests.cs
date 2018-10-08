using Microsoft.VisualStudio.TestTools.UnitTesting;
using JordanSdk.Network.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using JordanSdk.Network.Core;
using System.Net;

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

        static ISocket ipv4ServerClient;
        static ISocket ipv6ServerClient;

        static string serverAddress = "";
        string ipv6ServerAddress = "::1";
        const int PORT = 4884;
        #endregion

        #region Test / Class Initialization
        [TestInitialize]
        public void Initialize()
        {
           
            mevent = new System.Threading.ManualResetEvent(false);
            ipv4Server = new TcpProtocol();
            ipv4Server.Port = PORT;
            ipv4Server.OnConnectionRequested += (socket) =>
            {
                ipv4ServerClient = socket;
                mevent.Set();
            };
            ipv4Server.Address = serverAddress;
            ipv4Server.Listen();
            ipv6Server = new TcpProtocol();
            ipv6Server.Port = PORT;
            ipv6Server.Address = IPAddress.IPv6Any.ToString();
            mevent.Reset();
            ipv6Server.OnConnectionRequested += (socket) =>
            {
                ipv6ServerClient = socket;
                mevent.Set();
            };
            ipv6Server.Listen();
            ipv4Client = this.CreateIPV4ClientProtocol().Connect(serverAddress,PORT);
            mevent.WaitOne(1000);
            mevent.Reset();
            ipv6Client = this.CreateIPV6ClientProtocol().Connect(ipv6ServerAddress , PORT);
            mevent.WaitOne(1000);

        }

        [TestCleanup]
        public void TestCleanup()
        {
            ipv4Client.Disconnect();
            ipv6Client.Disconnect();
            ipv4Server.StopListening();
            ipv4Server.Dispose();
            ipv6Server.StopListening();
            ipv6Server.Dispose();
        }

        [ClassInitialize]
        public static void ClassInitialized(TestContext context)
        {
            //Based on multiple IP addresses configured in the network adapter.

            var selected = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(p => {
                return p.AddressFamily == AddressFamily.InterNetwork;
            }).Select(p => p.ToString());

            serverAddress = selected.First();
            

            BIG_BUFFER_DATA = new byte[3000];
            Random rnd = new Random();
            rnd.NextBytes(BIG_BUFFER_DATA);
            HUGE_BUFFER_DATA = new byte[Int16.MaxValue * 2];
            rnd.NextBytes(HUGE_BUFFER_DATA);
            
        }

        #endregion

        #region Test Cases

        [TestMethod(), TestCategory("TCPSocket (Send)")]
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

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public void SynchronousIPV4SendMidSizeBufferTest()
        {
            try
            {
               
                NetworkBuffer buffer = TestData.GetLargeBuffer();

                int sent = 0;
                byte[] sentData = null;
                while (null != (sentData = buffer.Read(TcpProtocol.BUFFER_SIZE)))
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

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public void SynchronousIPV4SendLargeBufferTest()
        {
            try
            {
              
                NetworkBuffer buffer = TestData.GetMidSizeBuffer();
                int sent = 0;
                byte[] sentData = null;
                while (null != (sentData = buffer.Read(TcpProtocol.BUFFER_SIZE)))
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


        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public void AsyncCallbackIPV4SendSmallBufferTest()
        {
            try
            {
                NetworkBuffer buffer = TestData.GetDummyStream();
                mevent.Reset();
                int bytesSent = 0;

                ipv4Client.SendAsync(buffer.ToArray(), (sent) =>
                {
                    bytesSent += sent;
                    var task = ipv4ServerClient.ReceiveAsync();
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

                NetworkBuffer buffer = TestData.GetLargeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                while (null != (sentData = buffer.Read(TcpProtocol.BUFFER_SIZE)))
                {
                    mevent.Reset();
                    ipv4Client.SendAsync(sentData, (sent) =>
                    {
                        bytesSent += sent;
                        var task = ipv4ServerClient.ReceiveAsync();
                        mevent.Set();

                    });
                    mevent.WaitOne(1000);
                }
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

                NetworkBuffer buffer = TestData.GetMidSizeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                while ((sentData = buffer.Read(TcpProtocol.BUFFER_SIZE)) != null)
                {
                    mevent.Reset();
                    ipv4Client.SendAsync(sentData, (sent) =>
                    {
                        bytesSent += sent;
                        ipv4ServerClient.ReceiveAsync();
                        mevent.Set();

                    });
                    mevent.WaitOne(1000);
                }
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

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public async Task AsyncTaskIPV4SendMidSizeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = TestData.GetLargeBuffer();

                byte[] sentData = null;
                int bytesSent = 0;
                while (null != (sentData = buffer.Read(TcpProtocol.BUFFER_SIZE)))
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

        [TestMethod(), TestCategory("TCPSocket (Send)")]
        public async Task AsyncTaskIPV4SendLargeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = TestData.GetMidSizeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                while (null != (sentData = buffer.Read(TcpProtocol.BUFFER_SIZE)))
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

        [TestMethod(), TestCategory("TCPSocket (Disconnect)")]
        public void DisconnectTest()
        {
            var socket = this.CreateIPV4ClientProtocol().Connect(serverAddress,PORT);
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            socket.Disconnect();
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("TCPSocket (Disconnect)")]
        public async Task AsyncTaskDisconnectTest()
        {
            var socket = this.CreateIPV4ClientProtocol().Connect(serverAddress,PORT);
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            await socket.DisconnectAsync();
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("TCPSocket (Disconnect)")]
        public void AsyncCallbackDisconnectTest()
        {
            mevent.Reset();
            var socket = this.CreateIPV4ClientProtocol().Connect(serverAddress,PORT);
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
            mevent.Reset();
            Task.Run(async () => { await Task.Delay(20); ipv4ServerClient.Send(TestData.GetDummyStream().ToArray()); });
            byte[] buffer = ipv4Client.Receive();
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Length > 0);
        }

        [TestMethod(), TestCategory("TCPSocket (Receive)")]
        public async Task AsyncTaskReceiveTest()
        {
            var sentAsync = Task.Run(async () => { await Task.Delay(20); ipv4ServerClient.Send(TestData.GetDummyStream().ToArray()); });
            byte[] buffer = await ipv4Client.ReceiveAsync();
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Length > 0);
        }

        [TestMethod(), TestCategory("TCPSocket (Receive)")]
        public void AsyncCallbackReceiveTest()
        {
            var sentAsync = Task.Run(async () => { await Task.Delay(20); ipv4ServerClient.Send(TestData.GetDummyStream().ToArray()); });
            byte[] received = null;
            mevent.Reset();
            ipv4Client.ReceiveAsync((buffer)=>
            {
                received = buffer;
                mevent.Set();
            });
            mevent.WaitOne();
            Assert.IsNotNull(received);
            Assert.IsTrue(received.Length > 0);
        }


        #endregion
    }
}
﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using JordanSdk.Network.Udp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using JordanSdk.Network.Core;
using System.Net;
using System.Threading;

namespace JordanSdk.Network.Udp.Tests
{
    /*
     In order for these test cases to work, is recommended to have multiple IP addresses on your machine.
         */


    [TestClass()]
    public class UdpSocketTests
    {

        #region Fields
       
        static UdpProtocol ipv4Server;
        static UdpProtocol ipv6Server;
        static ISocket ipv4Client;
        static ISocket ipv6Client;
        static ISocket ipv4ServerClient;
        static ISocket ipv6ServerClient;
        static string serverAddress = "";
        static string clientAddress = "";
        const string ipv6ServerAddress = "::1";
        const int PORT = 4884;
        List<ISocket> sockets = new List<ISocket>();
        #endregion

        #region Test / Class Initialization


        [TestInitialize]
        public void Initialize()
        {

            ManualResetEventSlim mevent = new ManualResetEventSlim(false);

            ipv4Server = new UdpProtocol();
            ipv4Server.Address = serverAddress;
            ipv4Server.Port = PORT;
            mevent.Reset();
            ipv4Server.OnConnectionRequested += (socket) =>
            {
                ipv4ServerClient = socket;
                mevent.Set();
            };
            ipv4Server.Listen();
            ipv6Server = new UdpProtocol();
            ipv6Server.Address = IPAddress.IPv6Any.ToString();
            ipv6Server.Port = PORT;
            ipv6Server.Listen();
            ipv6Server.OnConnectionRequested += (socket) =>
            {
                ipv6ServerClient = socket;
                mevent.Set();
            };
            ipv4Client = this.CreateIPV4ClientProtocol(clientAddress).Connect(serverAddress, PORT);
            mevent.Wait(1000);
            mevent.Reset();
            ipv6Client = this.CreateIPV6ClientProtocol(null).Connect(ipv6ServerAddress, PORT);
            mevent.Wait(1000);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ipv4Client.Disconnect();
            ipv6Client.Disconnect();
            foreach (ISocket socket in sockets)
                socket?.Disconnect();
            if(ipv4Server.Listening)
                ipv4Server.StopListening();
            ipv4Server.Dispose();
            if(ipv6Server.Listening)
                ipv6Server.StopListening();
            ipv6Server.Dispose();
        }

        [ClassInitialize]
        public static void ClassInitialized(TestContext context)
        {
            //Based on multiple IP addresses configured in the network adapter.
            var hostEntries = Dns.GetHostEntry(Dns.GetHostName());
            var selected = hostEntries.AddressList.Where(p => {
                return p.AddressFamily == AddressFamily.InterNetwork;
            }).Select(p=> p.ToString());
            serverAddress = selected.First();
            if (selected.Count() < 2)
            {
                //If we don't have multiple IP addresses lets try re-using the same IP
                //Not good for client/server testing at all, is a bad idea to fall in here
                clientAddress = serverAddress;
            }
            else
            {
                clientAddress = selected.Skip(1).First();
            }
            
        }

        #endregion

        #region Test Cases

        [TestMethod(), TestCategory("UdpSocket (Send)")]
        public void SynchronousIPV4SendSmallBufferTest()
        {
            try
            {
                NetworkBuffer buffer = TestData.GetDummyStream();
                int sent = ipv4Client.Send(buffer.ToArray());
                var task = ipv4ServerClient.ReceiveAsync();
                Assert.AreEqual(buffer.Size, sent, "Not all bytes were sent");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("UdpSocket (Send)")]
        public void SynchronousIPV4SendMidSizeBufferTest()
        {
            try
            {
               
                NetworkBuffer buffer = TestData.GetMidSizeBuffer();
                int sent = 0;
                byte[] sentData = null;
                while (null != (sentData = buffer.Read(UdpProtocol.BUFFER_SIZE)))
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

        [TestMethod(), TestCategory("UdpSocket (Send)")]
        public void SynchronousIPV4SendLargeBufferTest()
        {
            try
            {
                NetworkBuffer buffer = TestData.GetLargeBuffer();
                int sent = 0;
                byte[] sentData = null;
                while (null != (sentData = buffer.Read(UdpProtocol.BUFFER_SIZE)))
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


        [TestMethod(), TestCategory("UdpSocket (Send)")]
        public void AsyncCallbackIPV4SendSmallBufferTest()
        {
            try
            {

                ManualResetEventSlim mevent = new ManualResetEventSlim(false);
                NetworkBuffer buffer = TestData.GetDummyStream();
                mevent.Reset();
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

        [TestMethod(), TestCategory("UdpSocket (Send)")]
        public void AsyncCallbackIPV4SendMidSizeBufferTest()
        {
            try
            {
                ManualResetEventSlim mevent = new ManualResetEventSlim(false);
                NetworkBuffer buffer = TestData.GetMidSizeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                while (null != (sentData = buffer.Read(UdpProtocol.BUFFER_SIZE)))
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

        [TestMethod(), TestCategory("UdpSocket (Send)")]
        public void AsyncCallbackIPV4SendLargeBufferTest()
        {
            try
            {

                ManualResetEventSlim mevent = new ManualResetEventSlim(false);
                NetworkBuffer buffer = TestData.GetLargeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                while ((sentData = buffer.Read(UdpProtocol.BUFFER_SIZE)) != null)
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


        [TestMethod(), TestCategory("UdpSocket (Send)")]
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

        [TestMethod(), TestCategory("UdpSocket (Send)")]
        public async Task AsyncTaskIPV4SendMidSizeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = TestData.GetMidSizeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                while (null != (sentData = buffer.Read(UdpProtocol.BUFFER_SIZE)))
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

        [TestMethod(), TestCategory("UdpSocket (Send)")]
        public async Task AsyncTaskIPV4SendLargeBufferTest()
        {
            try
            {

                NetworkBuffer buffer = TestData.GetLargeBuffer();
                byte[] sentData = null;
                int bytesSent = 0;
                while (null != (sentData = buffer.Read(UdpProtocol.BUFFER_SIZE)))
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

        [TestMethod(), TestCategory("UdpSocket (Disconnect)")]
        public void DisconnectTest()
        {
            var socket = this.CreateIPV4ClientProtocol(clientAddress).Connect(serverAddress, PORT);
            Assert.IsNotNull(socket);
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            socket?.Disconnect();
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("UdpSocket (Disconnect)")]
        public async Task AsyncTaskDisconnectTest()
        {
            var socket = this.CreateIPV4ClientProtocol(clientAddress).Connect(serverAddress, PORT);
            Assert.IsNotNull(socket);
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            await socket.DisconnectAsync();
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("UdpSocket (Disconnect)")]
        public void AsyncCallbackDisconnectTest()
        {
            ManualResetEventSlim mevent = new ManualResetEventSlim(false);
            mevent.Reset();
            var socket = this.CreateIPV4ClientProtocol(clientAddress).Connect(serverAddress, PORT);
            Assert.IsNotNull(socket);
            Assert.IsTrue(socket.Connected, "Test is invalid because a connection could not be established.");
            socket.DisconnectAsync(()=>
            {
                mevent.Set();
            });
            mevent.Wait(1000);
            Assert.IsFalse(socket.Connected, "The connection is still open.");
        }

        [TestMethod(), TestCategory("UdpSocket (Receive)")]
        public void ReceiveTest()
        {
            Task.Run(async () => { await Task.Delay(20); ipv4ServerClient.Send(TestData.GetDummyStream().ToArray()); });
            byte[] buffer = ipv4Client.Receive();
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Length > 0);
        }

        [TestMethod(), TestCategory("UdpSocket (Receive)")]
        public async Task AsyncTaskReceiveTest()
        {
            var sentAsync = Task.Run(async () => { await Task.Delay(20); ipv4ServerClient.Send(TestData.GetDummyStream().ToArray()); });
            byte[] buffer = await ipv4Client.ReceiveAsync();
            Assert.IsNotNull(buffer);
            Assert.IsTrue(buffer.Length > 0);
        }

        [TestMethod(), TestCategory("UdpSocket (Receive)")]
        public void AsyncCallbackReceiveTest()
        {
            ManualResetEventSlim mevent = new ManualResetEventSlim(false);
            var sentAsync = Task.Run(async () => { await Task.Delay(20); ipv4ServerClient.Send(TestData.GetDummyStream().ToArray()); });
            byte[] received = null;
            mevent.Reset();
            ipv4Client.ReceiveAsync((buffer) =>
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
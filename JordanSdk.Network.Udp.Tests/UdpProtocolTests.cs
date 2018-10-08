using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JordanSdk.Network.Core;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;

namespace JordanSdk.Network.Udp.Tests
{
    [TestClass]
    public class UdpProtocolTests
    {
        #region Private Fields

        System.Threading.ManualResetEvent mevent;
        UdpProtocol ipv4Protocol;
        UdpProtocol ipv6Protocol;
        List<UdpSocket> sockets = new List<UdpSocket>();
        static string serverAddress = "";
        static string clientAddress = "";
        static string ipv6ServerAddress = "[::]";
        const int PORT = 4884;
        #endregion

        [ClassInitialize]
        public static void ClassInitialized(TestContext context)
        {

            //Based on multiple IP addresses configured in the network adapter.

            var selected = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(p => {
                return p.AddressFamily == AddressFamily.InterNetwork;
            }).Select(p => p.ToString());
            var selectediPV6 = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(p => {
                return p.AddressFamily == AddressFamily.InterNetworkV6;
            }).Select(p => p.ToString());
            ipv6ServerAddress = selectediPV6.First();
            serverAddress = selected.First();
            clientAddress = selected.Skip(1).First();

        }

        [TestInitialize]
        public void Initialize()
        {
            ipv4Protocol = new UdpProtocol();
            ipv4Protocol.Address = serverAddress;
            ipv4Protocol.Port = PORT;
            ipv6Protocol = new UdpProtocol();
            ipv6Protocol.Port = PORT;
            ipv6Protocol.Address = ipv6ServerAddress;
            mevent = new System.Threading.ManualResetEvent(true);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            foreach (UdpSocket socket in sockets)
                socket?.Disconnect();
            //if (ipv4Protocol != null && ipv4Protocol.Listening)
            ipv4Protocol.StopListening();
            //if (ipv6Protocol != null && ipv6Protocol.Listening)
            ipv6Protocol.StopListening();
        }

        [TestMethod(), TestCategory("UDPProcotol (Listen)")]
        public void ListenIPV4Test()
        {
            ipv4Protocol.Listen();
            Assert.IsTrue(ipv4Protocol.Listening, "Listening Flag was not turned on.");
        }



        [TestMethod(), TestCategory("UDPProcotol (Listen)")]
        public void ListenIPV6Test()
        {
            ipv6Protocol.Listen();
            Assert.IsTrue(ipv6Protocol.Listening, "Listening Flag was not turned on.");
        }

        [TestMethod(), TestCategory("UDPProcotol (Listen)")]
        public void ListenMultipleInstancesTest()
        {

            ipv4Protocol.Listen();
            ipv6Protocol.Listen();
            Assert.IsTrue(ipv6Protocol.Listening, "Listening Flag was not turned on for IPV6.");
            Assert.IsTrue(ipv4Protocol.Listening, "Listening Flag was not turned on for IPV4");
        }

        [TestMethod(), TestCategory("UDPProcotol (Stop Listening)")]
        public void StopListeningTest()
        {
            ipv4Protocol.Listen();
            ipv6Protocol.Listen();
            Assert.IsTrue(ipv6Protocol.Listening, "Listening Flag was not turned on for IPV6.");
            Assert.IsTrue(ipv4Protocol.Listening, "Listening Flag was not turned on for IPV4");
            ipv4Protocol.StopListening();
            ipv6Protocol.StopListening();
            Assert.IsFalse(ipv6Protocol.Listening, "Listening Flag was turned on for IPV6.");
            Assert.IsFalse(ipv4Protocol.Listening, "Listening Flag was turned on for IPV4");
        }

        [TestMethod(), TestCategory("UDPProcotol (Dispose)")]
        public void DisposeTest()
        {
            var ipv4Protocol = new UdpProtocol();
            ipv4Protocol.Port = PORT;
            ipv4Protocol.Listen();
            ipv4Protocol.Dispose();
            Assert.IsFalse(ipv4Protocol.Listening);
        }

        [TestMethod(), TestCategory("UDPProcotol (Connect)")]
        public void ConnectAsyncIPV4CallbackTest()
        {
            mevent.Reset();
            try
            {
                ipv4Protocol.Listen();
                UdpProtocol ipvClient = this.CreateIPV4ClientProtocol(clientAddress);
                UdpSocket udpSocket = null;
                ipvClient.ConnectAsync((socket) =>
                {
                    udpSocket = socket;
                    mevent.Set();
                },serverAddress,PORT);
                Assert.IsTrue(mevent.WaitOne(10000), "Connection callback never triggered callback.");
                sockets.Add(udpSocket);
                Assert.IsNotNull(udpSocket, "A connection could not be established.");
                Assert.IsTrue(udpSocket.Connected, "A connection could not be established.");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("UDPProcotol (Connect)")]
        public void ConnectAsyncIPV6CallbackTest()
        {
            mevent.Reset();
            try
            {
                ipv6Protocol.Listen();
                UdpProtocol ipvClient = this.CreateIPV6ClientProtocol(ipv6ServerAddress);
                UdpSocket udpSocket = null;
                ipvClient.ConnectAsync((socket) =>
                {
                    udpSocket = socket;
                    mevent.Set();
                }, ipv6ServerAddress, PORT);
                Assert.IsTrue(mevent.WaitOne(10000), "Connection never triggered callback.");
                sockets.Add(udpSocket);
                Assert.IsNotNull(udpSocket, "Callback did not receive a socket.");
                Assert.IsTrue(udpSocket.Connected, "A connection could not be established.");
                udpSocket.Disconnect();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("UDPProcotol (Connect)")]
        public async Task ConnectAsyncIPV4TaskTest()
        {

            try
            {
                ipv4Protocol.Listen();
                UdpProtocol ipvClient = this.CreateIPV4ClientProtocol(clientAddress);
                var udpSocket = await ipvClient.ConnectAsync(serverAddress, PORT);
                sockets.Add(udpSocket);
                Assert.IsNotNull(udpSocket);
                Assert.IsTrue(udpSocket.Connected);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("UDPProcotol (Connect)")]
        public async Task ConnectAsyncIPV6TaskTest()
        {
            try
            {
                ipv6Protocol.Listen();
                UdpProtocol ipvClient = this.CreateIPV6ClientProtocol(ipv6ServerAddress);
                var udpSocket = await ipvClient.ConnectAsync(ipv6ServerAddress, PORT);
                sockets.Add(udpSocket);
                Assert.IsNotNull(udpSocket);
                Assert.IsTrue(udpSocket.Connected);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }


        [TestMethod(), TestCategory("UDPProcotol (Connect)")]
        public void ConnectIPV4Test()
        {
            try
            {
                ipv4Protocol.Listen();
                UdpProtocol ipvClient = this.CreateIPV4ClientProtocol(clientAddress);
                var udpSocket = ipvClient.Connect(serverAddress, PORT);
                sockets.Add(udpSocket);
                Assert.IsNotNull(udpSocket);
                Assert.IsTrue(udpSocket.Connected, "A connection could not be established.");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("UDPProcotol (Connect)")]
        public void ConnectIPV6Test()
        {
            try
            {
                ipv6Protocol.Listen();
                UdpProtocol ipvClient = this.CreateIPV6ClientProtocol(ipv6ServerAddress);
                var udpSocket = ipvClient.Connect(ipv6ServerAddress, PORT);
                sockets.Add(udpSocket);
                Assert.IsNotNull(udpSocket);
                Assert.IsTrue(udpSocket.Connected, "A connection could not be established.");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("UDPProcotol (On Connection Requested Event)")]
        public void OnConnectionRequestedTest()
        {
            ipv4Protocol.Listen();
            mevent.Reset();
            bool eventInvoked = false;
            ipv4Protocol.OnConnectionRequested += (socket) =>
            {
                eventInvoked = true;
                mevent.Set();
            };
            UdpProtocol ipvClient = this.CreateIPV4ClientProtocol(clientAddress);
            UdpSocket clientSocket = ipvClient.Connect(serverAddress, PORT);
            sockets.Add(clientSocket);

            clientSocket.Send(TestData.GetDummyStream().ToArray());
            Assert.IsTrue(clientSocket.Connected);
            if(!mevent.WaitOne(10000))
                ipv4Protocol.GetDiagnostics();
            Assert.IsTrue(eventInvoked);
        }

    }
}

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JordanSdk.Network.Core;
using System.Threading.Tasks;

namespace JordanSdk.Network.Udp.Tests
{
    [TestClass]
    public class UdpProtocolTests
    {
        #region Private Fields

        System.Threading.ManualResetEvent mevent;
        UdpProtocol ipv4Protocol;
        UdpProtocol ipv6Protocol;

        #endregion

        [TestInitialize]
        public void Initialize()
        {
            ipv4Protocol = new UdpProtocol();
            ipv4Protocol.Port = 4884;
            ipv4Protocol.IPAddressKind = IPAddressKind.IPV4;
            ipv6Protocol = new UdpProtocol();
            ipv6Protocol.Port = 4884;
            ipv6Protocol.IPAddressKind = IPAddressKind.IPV6;
            mevent = new System.Threading.ManualResetEvent(true);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (ipv4Protocol != null && ipv4Protocol.Listening)
                ipv4Protocol.StopListening();
            if (ipv6Protocol != null && ipv6Protocol.Listening)
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
            ipv4Protocol.Port = 4884;
            ipv4Protocol.IPAddressKind = IPAddressKind.IPV4;
            ipv4Protocol.Listen();
            ipv4Protocol.Dispose();
            Assert.IsTrue(true);
        }

        [TestMethod(), TestCategory("UDPProcotol (Connect)")]
        public void ConnectAsyncIPV4CallbackTest()
        {
            mevent.Reset();
            try
            {
                ipv4Protocol.Listen();
                UdpProtocol ipvClient = this.CreateIPV4ClientProtocol();
                ipvClient.ConnectAsync((socket) =>
                {
                    Assert.IsTrue(socket.Connected, "A connection could not be established.");
                    mevent.Set();
                });
                mevent.WaitOne();
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
                UdpProtocol ipvClient = this.CreateIPV6ClientProtocol();
                ipvClient.ConnectAsync((socket) =>
                {
                    Assert.IsTrue(socket.Connected, "A connection could not be established.");
                    mevent.Set();
                });
                mevent.WaitOne();
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
                UdpProtocol ipvClient = this.CreateIPV4ClientProtocol();
                var udpSocket = await ipvClient.ConnectAsync();
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
                UdpProtocol ipvClient = this.CreateIPV6ClientProtocol();
                var udpSocket = await ipvClient.ConnectAsync();
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
                UdpProtocol ipvClient = this.CreateIPV4ClientProtocol();
                var udpSocket = ipvClient.Connect();
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
                UdpProtocol ipvClient = this.CreateIPV6ClientProtocol();
                var udpSocket = ipvClient.Connect();
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
            UdpProtocol ipvClient = this.CreateIPV4ClientProtocol();
            UdpSocket clientSocket = ipvClient.Connect();
            Assert.IsTrue(clientSocket.Connected);
            mevent.WaitOne(10000);
            Assert.IsTrue(eventInvoked);
        }

    }
}

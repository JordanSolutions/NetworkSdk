using Microsoft.VisualStudio.TestTools.UnitTesting;
using JordanSdk.Network.Core;
using System;
using System.Threading.Tasks;

namespace JordanSdk.Network.Tcp.Tests
{
    [TestClass()]
    public class TCPProtocolTests
    {
        #region Private Fields

        System.Threading.ManualResetEvent mevent;
        TcpProtocol ipv4Protocol;
        TcpProtocol ipv6Protocol;

        #endregion

        [TestInitialize]
        public void Initialize()
        {
            ipv4Protocol = new TcpProtocol();
            ipv4Protocol.Port = 4884;
            ipv4Protocol.IPAddressKind = IPAddressKind.IPV4;
            ipv6Protocol = new TcpProtocol();
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

        [TestMethod(), TestCategory("TCPProtocol (Listen)")]
        public void ListenIPV4Test()
        {
            ipv4Protocol.Listen();
            Assert.IsTrue(ipv4Protocol.Listening, "Listening Flag was not turned on.");
        }



        [TestMethod(), TestCategory("TCPProtocol (Listen)")]
        public void ListenIPV6Test()
        {
            ipv6Protocol.Listen();
            Assert.IsTrue(ipv6Protocol.Listening, "Listening Flag was not turned on.");
        }

        [TestMethod(), TestCategory("TCPProtocol (Listen)")]
        public void ListenMultipleInstancesTest()
        {

            ipv4Protocol.Listen();
            ipv6Protocol.Listen();
            Assert.IsTrue(ipv6Protocol.Listening, "Listening Flag was not turned on for IPV6.");
            Assert.IsTrue(ipv4Protocol.Listening, "Listening Flag was not turned on for IPV4");
        }

        [TestMethod(), TestCategory("TCPProtocol (Stop Listening)")]
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

        [TestMethod(), TestCategory("TCPProtocol (Dispose)")]
        public void DisposeTest()
        {
            var ipv4Protocol = new TcpProtocol();
            ipv4Protocol.Port = 4884;
            ipv4Protocol.IPAddressKind = IPAddressKind.IPV4;
            ipv4Protocol.Listen();
            ipv4Protocol.Dispose();
            Assert.IsTrue(true);
        }

        [TestMethod(), TestCategory("TCPProtocol (Connect)")]
        public void ConnectAsyncIPV4CallbackTest()
        {
            mevent.Reset();
            try
            {
                ipv4Protocol.Listen();
                TcpProtocol ipvClient = this.CreateIPV4ClientProtocol();
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

        [TestMethod(), TestCategory("TCPProtocol (Connect)")]
        public void ConnectAsyncIPV6CallbackTest()
        {
            mevent.Reset();
            try
            {
                ipv6Protocol.Listen();
                TcpProtocol ipvClient = this.CreateIPV6ClientProtocol();
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

        [TestMethod(), TestCategory("TCPProtocol (Connect)")]
        public async Task ConnectAsyncIPV4TaskTest()
        {

            try
            {
                ipv4Protocol.Listen();
                TcpProtocol ipvClient = this.CreateIPV4ClientProtocol();
                var tcpSocket = await ipvClient.ConnectAsync();
                Assert.IsNotNull(tcpSocket);
                Assert.IsTrue(tcpSocket.Connected);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPProtocol (Connect)")]
        public async Task ConnectAsyncIPV6TaskTest()
        {
            try
            {
                ipv6Protocol.Listen();
                TcpProtocol ipvClient = this.CreateIPV6ClientProtocol();
                var tcpSocket = await ipvClient.ConnectAsync();
                Assert.IsNotNull(tcpSocket);
                Assert.IsTrue(tcpSocket.Connected);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }


        [TestMethod(), TestCategory("TCPProtocol (Connect)")]
        public void ConnectIPV4Test()
        {
            try
            {
                ipv4Protocol.Listen();
                TcpProtocol ipvClient = this.CreateIPV4ClientProtocol();
                TcpSocket socket = ipvClient.Connect();
                Assert.IsTrue(socket.Connected, "A connection could not be established.");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPProtocol (Connect)")]
        public void ConnectIPV6Test()
        {
            try
            {
                ipv6Protocol.Listen();
                TcpProtocol ipvClient = this.CreateIPV6ClientProtocol();
                TcpSocket socket = ipvClient.Connect();
                Assert.IsTrue(socket.Connected, "A connection could not be established.");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("TCPProcotol (On Connection Requested Event)")]
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
            TcpProtocol ipvClient = this.CreateIPV4ClientProtocol();
            TcpSocket clientSocket = ipvClient.Connect();
            mevent.WaitOne(1000);
            Assert.IsTrue(eventInvoked);
        }

    }
}
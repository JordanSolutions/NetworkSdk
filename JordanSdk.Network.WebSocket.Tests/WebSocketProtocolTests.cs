using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JordanSdk.Network.WebSocket.Tests
{
    [TestClass]
    public class WebSocketProtocolTests
    {
        #region Private Fields

        System.Threading.ManualResetEvent mevent;
        WebSocketProtocol wsProtocol;
        static string hostAddress = "http://localhost/server/";
        static string serverAddress = "ws://localhost/server";


        const int PORT = 4884;
        #endregion

        [TestInitialize]
        public void Initialize()
        {
            wsProtocol = new WebSocketProtocol() { Port = PORT, Address = hostAddress };
            mevent = new System.Threading.ManualResetEvent(true);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (wsProtocol != null && wsProtocol.Listening)
                wsProtocol.StopListening();
        }

        [TestMethod(), TestCategory("Web Socket Protocol (Listen)")]
        public void ListenTest()
        {
            wsProtocol.Listen();
            Assert.IsTrue(wsProtocol.Listening, "Listening Flag was not turned on.");
        }
     
        [TestMethod(), TestCategory("Web Socket Protocol (Stop Listening)")]
        public void StopListeningTest()
        {
            wsProtocol.Listen();
            Assert.IsTrue(wsProtocol.Listening, "Listening Flag was not turned on for IPV4");
            wsProtocol.StopListening();
            Assert.IsFalse(wsProtocol.Listening, "Listening Flag was turned on for IPV4");
        }

        [TestMethod(), TestCategory("Web Socket Protocol (Dispose)")]
        public void DisposeTest()
        {
            var wsProtocol = new WebSocketProtocol();
            wsProtocol.Port = 4884;
            wsProtocol.Listen();
            wsProtocol.Dispose();
            Assert.IsFalse(wsProtocol.Listening);
        }

        [TestMethod(), TestCategory("Web Socket Protocol (Connect)")]
        public void ConnectAsyncCallbackTest()
        {
            mevent.Reset();
            try
            {
                wsProtocol.Listen();
                WebSocketProtocol ipvClient = this.CreateWSClientProtocol();
                bool connected = false;
                ipvClient.ConnectAsync((socket) =>
                {
                    connected = socket.Connected;
                    mevent.Set();
                }, serverAddress, PORT);
                mevent.WaitOne(10000);
                Assert.IsTrue(condition: connected, message: "A connection could not be established.");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }


        [TestMethod(), TestCategory("Web Socket Protocol (Connect)")]
        public async Task ConnectAsyncTaskTest()
        {

            try
            {
                wsProtocol.Listen();
                WebSocketProtocol ipvClient = this.CreateWSClientProtocol();
                var wsSocket = await ipvClient.ConnectAsync(serverAddress, PORT);
                Assert.IsNotNull(wsSocket);
                Assert.IsTrue(wsSocket.Connected);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }


        [TestMethod(), TestCategory("Web Socket Protocol (Connect)")]
        public void ConnectTest()
        {
            try
            {
                wsProtocol.Listen();
                WebSocketProtocol ipvClient = this.CreateWSClientProtocol();
                WebSocket socket = ipvClient.Connect(serverAddress, PORT);
                Assert.IsTrue(socket.Connected, "A connection could not be established.");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod(), TestCategory("Web Socket Protocol (On Connection Requested Event)")]
        public void OnConnectionRequestedTest()
        {
            wsProtocol.Listen();
            mevent.Reset();
            bool eventInvoked = false;
            wsProtocol.OnConnectionRequested += (socket) =>
            {
                eventInvoked = true;
                mevent.Set();
            };
            WebSocketProtocol ipvClient = this.CreateWSClientProtocol();
            WebSocket clientSocket = ipvClient.Connect(serverAddress, PORT);
            mevent.WaitOne(10000);
            Assert.IsTrue(eventInvoked);
        }
    }
}

using JordanSdk.Network.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.WebSocket.Tests
{
    static class TestExtensions
    {
        public static WebSocketProtocol CreateWSClientProtocol(this WebSocketProtocolTests test) => new WebSocketProtocol();

        public static WebSocketProtocol CreateWSClientProtocol(this TCPSocketTests test) => new WebSocketProtocol();

    }
}

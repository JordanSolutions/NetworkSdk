using JordanSdk.Network.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.Tcp.Tests
{
    static class TestExtensions
    {
        public static TcpProtocol CreateIPV4ClientProtocol(this TCPProtocolTests test) => new TcpProtocol();

        public static TcpProtocol CreateIPV4ClientProtocol(this TCPSocketTests test) => new TcpProtocol();

        public static TcpProtocol CreateIPV6ClientProtocol(this TCPProtocolTests test) => new TcpProtocol();

        public static TcpProtocol CreateIPV6ClientProtocol(this TCPSocketTests test) => new TcpProtocol();

    }
}

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
        public static TcpProtocol CreateIPV4ClientProtocol(this TCPProtocolTests test) => CreateIPV4ClientProtocol();

        public static TcpProtocol CreateIPV4ClientProtocol(this TCPSocketTests test) => CreateIPV4ClientProtocol();

        public static TcpProtocol CreateIPV6ClientProtocol(this TCPProtocolTests test) => CreateIPV6ClientProtocol();

        public static TcpProtocol CreateIPV6ClientProtocol(this TCPSocketTests test) => CreateIPV6ClientProtocol();

        public static NetworkBuffer CreateDummyStream(this TCPProtocolTests test) => GetDummyStream();

        public static NetworkBuffer CreateDummyStream(this TCPSocketTests test) => GetDummyStream();

        private static TcpProtocol CreateIPV4ClientProtocol()
        {
            return new TcpProtocol()
            {
                Address = "127.0.0.1",
                IPAddressKind = IPAddressKind.IPV4,
                Port = 4884
            };
        }


        private static TcpProtocol CreateIPV6ClientProtocol()
        {
            return new TcpProtocol()
            {
                Address = "::1",
                IPAddressKind = IPAddressKind.IPV6,
                Port = 4884
            };
        }

        private static NetworkBuffer GetDummyStream()
        {
            byte[] helloWorld = System.Text.Encoding.UTF8.GetBytes("Hello World.");
            return new NetworkBuffer(helloWorld.Length, helloWorld);
        }
    }
}

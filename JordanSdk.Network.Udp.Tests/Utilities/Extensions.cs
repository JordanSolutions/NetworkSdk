using JordanSdk.Network.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.Udp.Tests
{
    static class TestExtensions
    {
        public static UdpProtocol CreateIPV4ClientProtocol(this UdpProtocolTests test) => CreateIPV4ClientProtocol();

        public static UdpProtocol CreateIPV4ClientProtocol(this UdpSocketTests test) => CreateIPV4ClientProtocol();

        public static UdpProtocol CreateIPV6ClientProtocol(this UdpProtocolTests test) => CreateIPV6ClientProtocol();

        public static UdpProtocol CreateIPV6ClientProtocol(this UdpSocketTests test) => CreateIPV6ClientProtocol();

        public static NetworkBuffer CreateDummyStream(this UdpProtocolTests test) => GetDummyStream();

        public static NetworkBuffer CreateDummyStream(this UdpSocketTests test) => GetDummyStream();

        private static UdpProtocol CreateIPV4ClientProtocol()
        {
            return new UdpProtocol()
            {
                Address = "127.0.0.1",
                IPAddressKind = IPAddressKind.IPV4,
                Port = 4884
            };
        }


        private static UdpProtocol CreateIPV6ClientProtocol()
        {
            return new UdpProtocol()
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

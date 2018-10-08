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
        static Random rnd = new Random();
        public static UdpProtocol CreateIPV4ClientProtocol(this UdpProtocolTests test, string localAddress) => CreateIPV4ClientProtocol(localAddress);

        public static UdpProtocol CreateIPV4ClientProtocol(this UdpSocketTests test, string localAddress) => CreateIPV4ClientProtocol(localAddress);

        public static UdpProtocol CreateIPV6ClientProtocol(this UdpProtocolTests test, string localAddress) => CreateIPV6ClientProtocol(localAddress);

        public static UdpProtocol CreateIPV6ClientProtocol(this UdpSocketTests test, string localAddress) => CreateIPV6ClientProtocol(localAddress);

        private static UdpProtocol CreateIPV4ClientProtocol(string localAddress)
        {
            return new UdpProtocol()
            {
                Address = localAddress == null ? "127.0.0.1" : localAddress,
                Port = rnd.Next(10000, short.MaxValue - 1)
            };
        }


        private static UdpProtocol CreateIPV6ClientProtocol(string localAddress)
        {
            return new UdpProtocol()
            {
                Address = localAddress == null ? "::1" : localAddress,
                Port = rnd.Next(10000, short.MaxValue - 1)
            };
        }
      
    }
}

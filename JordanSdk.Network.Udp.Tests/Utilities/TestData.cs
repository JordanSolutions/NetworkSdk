using JordanSdk.Network.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.Udp.Tests
{
    static class TestData
    {
        public static byte[] MID_BUFFER;
        public static byte[] LARGE_BUFFER;

        static TestData()
        {
            MID_BUFFER = new byte[30000];
            Random rnd = new Random();
            rnd.NextBytes(MID_BUFFER);
            LARGE_BUFFER = new byte[Int16.MaxValue * 4];
            rnd.NextBytes(LARGE_BUFFER);
        }

        public static NetworkBuffer GetDummyStream()
        {
            byte[] helloWorld = System.Text.Encoding.UTF8.GetBytes("Hello World.");
            return new NetworkBuffer(helloWorld.Length, helloWorld);
        }

        public static NetworkBuffer GetLargeBuffer() => new NetworkBuffer(TestData.LARGE_BUFFER.Length, TestData.LARGE_BUFFER);


        public static NetworkBuffer GetMidSizeBuffer() => new NetworkBuffer(TestData.MID_BUFFER.Length, TestData.MID_BUFFER);
    }
}

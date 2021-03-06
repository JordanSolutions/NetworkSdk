﻿using JordanSdk.Network.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.Core.Tests
{
    static class TestData
    {
        public static byte[] BIG_BUFFER_DATA;
        public static byte[] FAIR_BUFFER_DATA;

        static TestData()
        {
            BIG_BUFFER_DATA = new byte[30000];
            Random rnd = new Random();
            rnd.NextBytes(BIG_BUFFER_DATA);
            FAIR_BUFFER_DATA = new byte[Int16.MaxValue * 4];
            rnd.NextBytes(FAIR_BUFFER_DATA);
        }

        public static NetworkBuffer GetDummyStream()
        {
            byte[] helloWorld = System.Text.Encoding.UTF8.GetBytes("Hello World.");
            return new NetworkBuffer(helloWorld.Length, helloWorld);
        }

        public static NetworkBuffer GetFairStream() => new NetworkBuffer(TestData.FAIR_BUFFER_DATA.Length, TestData.FAIR_BUFFER_DATA);


        public static NetworkBuffer GetBigStream() => new NetworkBuffer(TestData.BIG_BUFFER_DATA.Length, TestData.BIG_BUFFER_DATA);
    }
}

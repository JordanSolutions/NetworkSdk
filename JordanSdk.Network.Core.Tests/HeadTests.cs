using System;
using System.Linq;
using System.Net.Sockets;
using JordanSdk.Network.Core;
using JordanSdk.Network.Core.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JordanSdk.Network.Core.Tests
{
    [TestClass]
    public class HeadTests
    {
        INetworkBuffer small = TestData.GetDummyStream();
        INetworkBuffer medium = TestData.GetDummyStream();
        INetworkBuffer large = TestData.GetDummyStream();

        [TestInitialize]
        public void Initialize()
        {
            small = TestData.GetDummyStream();
            medium = TestData.GetBigStream();
            large = TestData.GetHugeStream();
        }

        [TestMethod, TestCategory("Head (Constructor)")]
        public void ConstructorSmallStream()
        {
            
            Head head = new Head(small, 8192);
            Assert.IsNotNull(head.Next);
            Assert.IsNotNull(head.Id);
            Assert.IsTrue(head.PackageCount > 0);
            Assert.IsTrue(Enumerable.SequenceEqual(head.Checksum, small.GetChecksum()));
        }

        [TestMethod, TestCategory("Head (Constructor)")]
        public void ConstructorBigStream()
        {
           
            Head head = new Head(medium, 8192);
            Assert.IsNotNull(head.Next);
            Assert.IsNotNull(head.Id);
            Assert.IsTrue(head.PackageCount > 0);
            Assert.IsTrue(Enumerable.SequenceEqual(head.Checksum, medium.GetChecksum()));
        }

        [TestMethod, TestCategory("Head (Constructor)")]
        public void ConstructorHugeStream()
        {
            Head head = new Head(large, 8192);
            Assert.IsNotNull(head.Next);
            Assert.IsNotNull(head.Id);
            Assert.IsTrue(head.PackageCount > 0);
            Assert.IsTrue(Enumerable.SequenceEqual(head.Checksum, large.GetChecksum()));
        }


    }
}

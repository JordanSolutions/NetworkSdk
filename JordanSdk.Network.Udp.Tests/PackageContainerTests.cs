using System;
using System.Collections.Generic;
using System.Linq;
using JordanSdk.Network.Core;
using JordanSdk.Network.Udp.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JordanSdk.Network.Udp.Packages.Tests
{
    [TestClass]
    public class PackageContainerTests
    {
        INetworkBuffer small = null;
        INetworkBuffer medium = null;
        INetworkBuffer large = null;

        [TestInitialize]
        public void Initialize()
        {
            small = TestData.GetDummyStream();
            medium = TestData.GetBigStream();
            large = TestData.GetHugeStream();
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseSmall()
        {
            Package package = new Head(small);
            byte[] checksum = (package as Head).Checksum;
            PackageContainer container = new PackageContainer();
            while (package != null)
            {
                container.Parse(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseFail()
        {
            Package package1 = new Head(small);
            Package package2 = new Head(medium);
            PackageContainer container = new PackageContainer();
            container.Parse(package1.Pack());
            Assert.IsFalse(container.Parse(package2.Pack()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseMedium()
        {
            Package package = new Head(medium);
            byte[] checksum = (package as Head).Checksum;
            PackageContainer container = new PackageContainer();
            while (package != null)
            {
                container.Parse(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseMediumUnordered()
        {
            Package package = new Head(medium);
            List<Package> packages = new List<Package>();
            byte[] checksum = (package as Head).Checksum;
            PackageContainer container = new PackageContainer();
            while (package != null)
            {
                packages.Add(package);
                package = package.Next;
            }
            Random rnd = new Random();
            var shuffled = packages.OrderBy(x => rnd.Next());
            foreach (Package p in shuffled)
                container.Parse(p.Pack());
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseLarge()
        {
            Package package = new Head(large);
            byte[] checksum = (package as Head).Checksum;
            PackageContainer container = new PackageContainer();
            while (package != null)
            {
                container.Parse(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseLargeUnordered()
        {
            Package package = new Head(large);
            List<Package> packages = new List<Package>();
            byte[] checksum = (package as Head).Checksum;
            PackageContainer container = new PackageContainer();
            while (package != null)
            {
                packages.Add(package);
                package = package.Next;
            }
            Random rnd = new Random();
            var shuffled = packages.OrderBy(x => rnd.Next());
            foreach (Package p in shuffled)
                container.Parse(p.Pack());
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (ToBuffer)")]
        public void ToBufferSmall()
        {
            Package package = new Head(small);
            byte[] checksum = (package as Head).Checksum;
            PackageContainer container = new PackageContainer();
            while (package != null)
            {
                container.Parse(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            INetworkBuffer result = container.ToBuffer();
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, result.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (ToBuffer)")]
        public void ToBufferMedium()
        {
            Package package = new Head(medium);
            byte[] checksum = (package as Head).Checksum;
            PackageContainer container = new PackageContainer();
            while (package != null)
            {
                container.Parse(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            INetworkBuffer result = container.ToBuffer();
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, result.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (ToBuffer)")]
        public void ToBufferLarge()
        {
            Package package = new Head(large);
            byte[] checksum = (package as Head).Checksum;
            PackageContainer container = new PackageContainer();
            while (package != null)
            {
                container.Parse(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            INetworkBuffer result = container.ToBuffer();
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, result.GetChecksum()));
        }

    }
}

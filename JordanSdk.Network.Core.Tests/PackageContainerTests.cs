using System;
using System.Collections.Generic;
using System.Linq;
using JordanSdk.Network.Core;
using JordanSdk.Network.Core.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JordanSdk.Network.Core.Tests
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
            Package package = new Head(small, 8192);
            byte[] checksum = (package as Head).Checksum;
            PackageReader container = new PackageReader();
            while (package != null)
            {
                container.Read(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseFail()
        {
            Package package1 = new Head(small, 8192);
            Package package2 = new Head(medium, 8192);
            PackageReader container = new PackageReader();
            container.Read(package1.Pack());
            Assert.IsFalse(container.Read(package2.Pack()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseMedium()
        {
            Package package = new Head(medium, 8192);
            byte[] checksum = (package as Head).Checksum;
            PackageReader container = new PackageReader();
            while (package != null)
            {
                container.Read(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseMediumUnordered()
        {
            Package package = new Head(medium, 8192);
            List<Package> packages = new List<Package>();
            byte[] checksum = (package as Head).Checksum;
            PackageReader container = new PackageReader();
            while (package != null)
            {
                packages.Add(package);
                package = package.Next;
            }
            Random rnd = new Random();
            var shuffled = packages.OrderBy(x => rnd.Next());
            foreach (Package p in shuffled)
                container.Read(p.Pack());
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseLarge()
        {
            Package package = new Head(large, 8192);
            byte[] checksum = (package as Head).Checksum;
            PackageReader container = new PackageReader();
            while (package != null)
            {
                container.Read(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (Parse)")]
        public void ParseLargeUnordered()
        {
            Package package = new Head(large, 8192);
            List<Package> packages = new List<Package>();
            byte[] checksum = (package as Head).Checksum;
            PackageReader container = new PackageReader();
            while (package != null)
            {
                packages.Add(package);
                package = package.Next;
            }
            Random rnd = new Random();
            var shuffled = packages.OrderBy(x => rnd.Next());
            foreach (Package p in shuffled)
                container.Read(p.Pack());
            Assert.IsTrue(container.IsComplete());
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, container.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (ToBuffer)")]
        public void ToBufferSmall()
        {
            Package package = new Head(small, 8192);
            byte[] checksum = (package as Head).Checksum;
            PackageReader container = new PackageReader();
            while (package != null)
            {
                container.Read(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            INetworkBuffer result = container.ToBuffer();
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, result.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (ToBuffer)")]
        public void ToBufferMedium()
        {
            Package package = new Head(medium, 8192);
            byte[] checksum = (package as Head).Checksum;
            PackageReader container = new PackageReader();
            while (package != null)
            {
                container.Read(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            INetworkBuffer result = container.ToBuffer();
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, result.GetChecksum()));
        }

        [TestMethod, TestCategory("PackageContainer (ToBuffer)")]
        public void ToBufferLarge()
        {
            Package package = new Head(large, 8192);
            byte[] checksum = (package as Head).Checksum;
            PackageReader container = new PackageReader();
            while (package != null)
            {
                container.Read(package.Pack());
                package = package.Next;
            }
            Assert.IsTrue(container.IsComplete());
            INetworkBuffer result = container.ToBuffer();
            Assert.IsTrue(Enumerable.SequenceEqual(checksum, result.GetChecksum()));
        }

    }
}

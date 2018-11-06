using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JordanSdk.Diagnostic.Tests
{
    [TestClass]
    public class DefaultLogManagerTests
    {
       

        [TestMethod(),TestCategory("Diagnostic (Default Log Manager)")]
        public void Singleton()
        {
            DefaultLogManager manager = DefaultLogManager.Instance;
            Assert.IsNotNull(manager);
        }

        [TestMethod, TestCategory("Diagnostic (Default Log Manager)")]
        public void LogSync()
        {
            DefaultLogManager.Instance.Log<System.Tuple<string, string>>(new Tuple<string, string>("test", "test"));
            Assert.IsNotNull(DefaultLogManager.Instance.LogPath);
            Assert.IsTrue(System.IO.File.Exists(DefaultLogManager.Instance.LogPath));
        }

        [TestMethod, TestCategory("Diagnostic (Default Log Manager)")]
        public async Task LogAsync()
        {
            await DefaultLogManager.Instance.LogAsync<System.Tuple<string, string>>(new Tuple<string, string>("test", "test"));
            Assert.IsNotNull(DefaultLogManager.Instance.LogPath);
            Assert.IsTrue(System.IO.File.Exists(DefaultLogManager.Instance.LogPath));
        }

        [TestMethod, TestCategory("Diagnostic (Default Log Manager)")]
        public void LogException()
        {
            DefaultLogManager.Instance.LogException<Exception>(new Exception("This is a test"));
            Assert.IsNotNull(DefaultLogManager.Instance.LogPath);
            Assert.IsTrue(System.IO.File.Exists(DefaultLogManager.Instance.LogPath));
        }

        [TestMethod, TestCategory("Diagnostic (Default Log Manager)")]
        public async Task LogExceptionAsync()
        {
            await DefaultLogManager.Instance.LogExceptionAsync<Exception>(new Exception("This is a test"));
            Assert.IsNotNull(DefaultLogManager.Instance.LogPath);
            Assert.IsTrue(System.IO.File.Exists(DefaultLogManager.Instance.LogPath));
        }

        [TestMethod, TestCategory("Diagnostic (Default Log Manager)")]
        public void LogMultipleThreads()
        {
            var result = Parallel.For(0, 100, (i) =>
              {
                  DefaultLogManager.Instance.Log<System.Tuple<string, string>>(new Tuple<string, string>("test", "test"));
                  Assert.IsNotNull(DefaultLogManager.Instance.LogPath);
                  Assert.IsTrue(System.IO.File.Exists(DefaultLogManager.Instance.LogPath));
              });
            Assert.IsTrue(result.IsCompleted);
        }

        [TestMethod, TestCategory("Diagnostic (Default Log Manager)")]
        public async Task LogMultipleThreadsAsync()
        {
            try
            {
                Task[] logs = new Task[100];
                for (int i = 0; i < 100; i++)
                {
                    logs[i] = DefaultLogManager.Instance.LogAsync<System.Tuple<string, string>>(new Tuple<string, string>("test", "test"));
                };
                await Task.WhenAll(logs);
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail();
            }
        }
    }
}

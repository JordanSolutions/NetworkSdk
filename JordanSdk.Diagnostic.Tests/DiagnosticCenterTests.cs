using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JordanSdk.Diagnostic.Tests
{
    [TestClass]
    public class DiagnosticCenterTests
    {
        [TestMethod, TestCategory("Diagnostic (Diagnostic Center)")]
        public void RegisterLogManager()
        {
            DiagnosticCenter.Instance.RegisterLogManager(DefaultLogManager.Instance);
            Assert.IsNotNull(DiagnosticCenter.Instance.Log);
        }

        [TestMethod, TestCategory("Diagnostic (Diagnostic Center)")]
        public void ClearRegisterLogManager()
        {
            DiagnosticCenter.Instance.RegisterLogManager(null);
            Assert.IsNull(DiagnosticCenter.Instance.Log);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Diagnostic
{
    public class DiagnosticCenter
    {
        #region Private Fields
        private static readonly Lazy<DiagnosticCenter> instance =
        new Lazy<DiagnosticCenter>(() => new DiagnosticCenter());
        private ILogManager log = null;
        #endregion

        #region Public
        public static DiagnosticCenter Instance { get
            {
                return instance.Value;
            }
        }

        public ILogManager Log { get { return log; } }

        #endregion
        public void RegisterLogManager(ILogManager manager)
        {
            log = manager;
        }
    }
}

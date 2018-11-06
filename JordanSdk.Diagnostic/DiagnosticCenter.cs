using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Diagnostic
{

    /// <summary>
    /// Class used by Jordan SDK in order to attempt to record exceptions that are generally not propagated, as well as for any other informational logs.
    /// </summary>
    public class DiagnosticCenter
    {
        #region Private Fields
        private static readonly Lazy<DiagnosticCenter> instance = new Lazy<DiagnosticCenter>(() => new DiagnosticCenter(), true);
        private ILogManager log = null;
        #endregion

        #region Public
        /// <summary>
        /// Singleton Diagnostic Center Instance 
        /// </summary>
        public static DiagnosticCenter Instance { get
            {
                return instance.Value;
            }
        }

        /// <summary>
        /// Instance of the log manager register with the Diagnostic Center
        /// </summary>
        public ILogManager Log { get { return log; } }

        #endregion

        /// <summary>
        /// Registers a log manager that will be used internally to record errors by any JordanSdk, you can also unregister a log manager by simply passing null to this function.
        /// </summary>
        /// <param name="manager"></param>
        public void RegisterLogManager(ILogManager manager)
        {
            log = manager;
        }
    }
}

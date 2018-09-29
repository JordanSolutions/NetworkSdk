using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Diagnostic
{
    public interface ILogManager
    {
        /// <summary>
        /// When implemented, this dedicated function is used to log exceptions.
        /// </summary>
        /// <typeparam name="T">Any type equal or derived from Exception.</typeparam>
        /// <param name="exception">Exception to be recorded.</param>
        void LogException<T>(T exception) where T : Exception;

        /// <summary>
        /// When implemented, this generic function allows for logging any kind of data as envision by the provider implementation details.
        /// </summary>
        /// <typeparam name="T">Type of data to be recorded.</typeparam>
        /// <param name="data">Data to be recorded.</param>
        void Log<T>(T data);
    }
}

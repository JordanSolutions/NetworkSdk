using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JordanSdk.Diagnostic
{

    /// <summary>
    /// Simple log manager implementation that outputs to a fixed file named JordanSdk.log. Uses locks to control only one thread can write to the file at any given time.
    /// </summary>
    public class DefaultLogManager : ILogManager
    {

        #region Private Fields
        private static readonly Lazy<DefaultLogManager> instance =
         new Lazy<DefaultLogManager>(() => new DefaultLogManager(), true);
        SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private System.IO.FileStream log;
        private string filePath;
        #endregion

        #region Constructor / Destructor

        private DefaultLogManager() {
            filePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:\\", "").Replace("file:///","")),"JordanSdk.log");
            log = System.IO.File.Open(filePath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write, System.IO.FileShare.Read);
        }

        #endregion

        #region Properties


        /// <summary>
        /// Default Log Manager instance 
        /// </summary>
        public static DefaultLogManager Instance => instance.Value;

        /// <summary>
        /// Returns the path to the log file.
        /// </summary>
        /// <returns></returns>
        public string LogPath => filePath;

        #endregion

        #region Public Functions

        /// <summary>
        /// Writes data to the log synchronously.
        /// </summary>
        /// <typeparam name="T">Type of Data</typeparam>
        /// <param name="data">Data to be written.</param>
        /// <remarks>This function will use Object.ToString in order to write data to the logs. To output formatted information, make sure to override the ToString inherited function.</remarks>
        public void Log<T>(T data)
        {
            if (data == null)
                return;
            Write(data is string ? data as string : data.ToString());
        }

        /// <summary>
        /// Writes data to the log asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of Data</typeparam>
        /// <param name="data">Data to be written.</param>
        /// <remarks>This function will use Object.ToString in order to write data to the logs. To output formatted information, make sure to override the ToString inherited function.</remarks>
        public async Task LogAsync<T>(T data)
        {
            if (data == null)
                return;
            await WriteAsync(data is string ? data as string : data.ToString());
        }

        /// <summary>
        /// Writes the exception to the log synchronously.
        /// </summary>
        /// <typeparam name="T">Type of exception</typeparam>
        /// <param name="exception">Exception object to be written to the log.</param>
        public void LogException<T>(T exception) where T : Exception
        {
            StringBuilder _logEntry = new StringBuilder(exception.Message);
            _logEntry.AppendLine(exception.StackTrace ?? "No Stack Trace");
            Write(_logEntry.ToString());

        }

        /// <summary>
        /// Writes the exception to the log asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of exception</typeparam>
        /// <param name="exception">Exception object to be written to the log.</param>
        public async Task LogExceptionAsync<T>(T exception) where T : Exception
        {
            StringBuilder _logEntry = new StringBuilder(exception.Message);
            _logEntry.AppendLine(exception.StackTrace ?? "No Stack Trace");
            await WriteAsync(_logEntry.ToString());
        }


        #endregion

        #region Private Functions

        private async Task WriteAsync(string data)
        {
            byte[] _data = System.Text.Encoding.UTF8.GetBytes(data.ToString());
            await _writeLock.WaitAsync();
            try
            {
                await log.WriteAsync(_data, 0, _data.Length);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private void Write(string data)
        {
            byte[] _data = System.Text.Encoding.UTF8.GetBytes(data.ToString());
            _writeLock.Wait();
            try
            {
                log.Write(_data, 0, _data.Length);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                _writeLock.Release();
            }
           
        }

        #endregion

    }
}

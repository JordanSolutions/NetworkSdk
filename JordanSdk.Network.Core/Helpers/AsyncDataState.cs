using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// Helper class used for passing state data internally on several asynchronous functions, inheriting from Async Callback in order to take advantage of the Action declared on parent class. 
    /// </summary>
    /// <typeparam name="T">Type of State Property.</typeparam>
    /// <typeparam name="U">Type of Data Property.</typeparam>
    public class AsyncDataState <T, U> : AsyncCallbackState<T>
    {
        /// <summary>
        /// Additional state data passed to asynchronous functions.
        /// </summary>
        public U Data { get; set; }
    }
}

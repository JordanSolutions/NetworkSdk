using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// Helper class used for passing state data internally on several asynchronous functions. 
    /// </summary>
    /// <typeparam name="T">Type of State Property.</typeparam>
    /// <typeparam name="U">Type of Data Property.</typeparam>
    public class AsyncTupleState <T, U> : AsyncState<T>
    {
        /// <summary>
        /// Additional state data.
        /// </summary>
        public U Data { get; set; }
    }
}

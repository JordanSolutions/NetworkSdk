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
    /// <typeparam name="V">Type of the Complement property.</typeparam>
    public class AsyncTripletState<T, U, V> : AsyncTupleState<T, U>
    {
        /// <summary>
        /// Yet another generic storage element for states that need more than two properties.
        /// </summary>
        public V Complement { get; set; }
    }
}

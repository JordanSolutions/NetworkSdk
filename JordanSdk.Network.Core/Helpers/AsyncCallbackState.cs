using System;
using System.Net.Sockets;

namespace JordanSdk.Network.Core
{

    /// <summary>
    /// Helper class used for boxing an asynchronous state object containing a callback action.
    /// </summary>
    /// <typeparam name="T">Type of parameter used by Callback</typeparam>
    public class AsyncCallbackState<T> : AsyncState
    {
        /// <summary>
        /// Used to signal completion for the most part by internal send / receive / connected asynchronous requests.
        /// </summary>
        public Action<T> Callback { get; set; }

    }
}

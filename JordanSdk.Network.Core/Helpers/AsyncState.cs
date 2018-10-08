using System.Net.Sockets;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// Base asynchronous state object passed back and forth between several asynchronous functions defined in the Socket class, in order to box an System.Net.Socket object.
    /// </summary>
    public class AsyncState
    {
        /// <summary>
        /// Socket object.
        /// </summary>
        public Socket Socket { get; set; }
    }
}

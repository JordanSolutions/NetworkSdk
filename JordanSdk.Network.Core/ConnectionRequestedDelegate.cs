using System;
using System.Collections.Generic;
using System.Text;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// Used to trigger an event when users connect to an instance of a protocol playing the role of a socket server.
    /// </summary>
    /// <param name="socket">ISocket connected.</param>
    public delegate void SocketConnectedDelegate(ISocket socket);
}

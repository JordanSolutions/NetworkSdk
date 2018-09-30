using System;
using System.Collections.Generic;
using System.Text;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// Event invoked when the connection is lost or purposely closed.
    /// </summary>
    /// <param name="socket">Disconnected socket instance.</param>
    public delegate void DisconnectedDelegate(ISocket socket);
}

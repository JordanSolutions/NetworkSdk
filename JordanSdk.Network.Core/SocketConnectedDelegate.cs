﻿using System;
using System.Collections.Generic;
using System.Text;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// Used to trigger an event when users connect to an instance of a protocol playing the server role.
    /// </summary>
    /// <param name="socket">ISocket connected.</param>
    public delegate void SocketConnectedDelegate(ISocket socket);
}

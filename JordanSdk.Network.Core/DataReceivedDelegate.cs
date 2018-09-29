using System;
using System.Collections.Generic;
using System.Text;

namespace JordanSdk.Network.Core
{
    public delegate void DataReceivedDelegate(ISocket socket, byte[] data);
}

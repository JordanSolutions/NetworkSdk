using System;
using System.Collections.Generic;
using System.Text;

namespace JordanSdk.Network
{
    public enum ProtocolKind
    {
        Tcp=1,
        Udp=2,
        WebSocket=4,
        All = Tcp | Udp | WebSocket
            
    }
}

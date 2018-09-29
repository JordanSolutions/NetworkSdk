using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.TCP
{
    internal class AsyncState
    {
        public Socket Socket { get; set; }

        public object CallBack { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.TCP
{
    internal class GenericAsyncState <T> : AsyncState
    {
        public T Data { get; set; }

    }
}

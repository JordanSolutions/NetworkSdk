using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.TCP
{
    internal class GenericInOutAsyncState<T, U> : AsyncState
    {
        public T In { get; set; }

        public U Out { get; set; }
    }
}

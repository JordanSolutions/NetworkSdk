using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.Core
{
    public class AsyncTupleState <T, U> : AsyncState<T>
    {
        public U Data { get; set; }

    }
}

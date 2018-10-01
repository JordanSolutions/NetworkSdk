using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JordanSdk.Network.Core
{
    public class AsyncTripletState<T, U, V> : AsyncTupleState<T, U>
    {
        public V Complement { get; set; }
    }
}

using System.Net.Sockets;

namespace JordanSdk.Network.Core
{
    public class AsyncState<T>
    {
        public T State { get; set; }

        public object CallBack { get; set; }

    }
}

using System.Net.Sockets;

namespace JordanSdk.Network.Core
{
    public class AsyncState<T>
    {
        /// <summary>
        /// State data.
        /// </summary>
        public T State { get; set; }

        /// <summary>
        /// State object used to signal completion. This is currently used to box one of two types, and Action object or a Task Completion Source object.
        /// </summary>
        public object CallBack { get; set; }

    }
}

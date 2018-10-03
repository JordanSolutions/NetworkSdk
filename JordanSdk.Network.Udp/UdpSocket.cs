using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using JordanSdk.Network.Core;
using System.Net;
using JordanSdk.Network.Udp.Packages;
using System.Linq;

namespace JordanSdk.Network.Udp
{
    public class UdpSocket : ISocket
    {
        #region Private Fields
        Socket socket;
        EndPoint endPoint;
        string token;
        bool connected = false;
        #endregion

        #region Events
        /// <summary>
        /// Event invoked when the connection is lost or purposely closed.
        /// </summary>
        public event DisconnectedDelegate OnSocketDisconnected;

        #endregion

        #region Public Properties

        /// <summary>
        /// This property indicates the connected state of your socket.
        /// </summary>
        public bool Connected { get { return connected; } internal set { connected = value; } }

        /// <summary>
        /// This property contains a unique identifier allocated by the server.
        /// </summary>
        public string Token { get { return token; } }

        #endregion

        #region Constructor

        internal UdpSocket(Socket socket, IPEndPoint endPoint, string token)
        {
            if (socket == null)
                throw new ArgumentNullException("socket", "Socket can not be null.");
            else if (endPoint == null)
                throw new ArgumentNullException("endPoint", "Client endpoint can not be null.");
            this.socket = socket;
            this.token = token;
            this.endPoint = endPoint;
        }



        #endregion

        #region ISocket

        #region Connection Management

        public void Disconnect()
        {

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            Connected = false;
            OnSocketDisconnected?.Invoke(this);
        }

        public async Task DisconnectAsync()
        {
            await Task.Run(() =>
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Connected = false;
                OnSocketDisconnected?.Invoke(this);
            });
        }

        public void DisconnectAsync(Action callback)
        {
            Task.Run(() =>
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Connected = false;
                callback?.Invoke();
                OnSocketDisconnected?.Invoke(this);
            });
        }

        #endregion

        #region Sending

        /// <summary>
        /// Use this function to send a buffer over the network. This method blocks until all data in buffer is sent.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes sent.</returns>
        public int Send(INetworkBuffer data)
        {
            int bytesSent = 0;
            var clone = data.Clone();
            clone.ResetPosition();
            Package package = new Head(clone);
            while(package != null)
            {
                var _data = package.Pack();
                int sent = socket.SendTo(_data, endPoint);
                if (_data.Length == sent)
                    bytesSent += package.Data?.Length ?? 0;
                package = package.Next;
            }
            return bytesSent;
        }

        /// <summary>
        /// Use this function to send data over the network asynchronously.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes written to the network.</returns>
        public async Task<int> SendAsync(INetworkBuffer data)
        {
            TaskCompletionSource<int> task = new TaskCompletionSource<int>();
            //Need to create an immutable copy if the total count of bytes is greater than buffer size.
            var clone = data.Size > UdpProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            Package package = new Head(clone);
            var _data = package.Pack();
            var iresult = socket.BeginSendTo(_data, 0, _data.Length, 0, endPoint, SendCallback, new AsyncTripletState<Socket, Package, int>() { State = socket, Data = package, Complement = 0, CallBack = task });
            return await task.Task;
        }

        /// <summary>
        /// Use this function to send data over the network asynchronously. This method will invoke the provided action once the operation completes in order to provide feedback.
        /// </summary>
        /// <param name="data">INetworkBuffer containing the data to be sent.</param>
        /// <param name="callback">Callback invoked once the write operation concludes, containing the amount of bytes sent through the network.</param>
        public void SendAsync(INetworkBuffer data, Action<int> callback)
        {
            //Need to create an immutable copy if the total count of bytes is greater than buffer size.
            var clone = data.Size > UdpProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            Package package = new Head(clone);
            var _data = package.Pack();
            var iresult = socket.BeginSendTo(_data, 0, _data.Length, 0, endPoint, SendCallback, new AsyncTripletState<Socket, Package, int>() { State = socket, Data = package, Complement = 0, CallBack = callback });
        }

        #endregion

        #region Receiving Data

        /// <summary>
        /// Use this function to receive data from the network asynchronously. This function will invoke the provided action once data is received.
        /// </summary>
        /// <param name="callback">Callback to be invoked when data is received.</param>
        public void ReceiveAsync(Action<INetworkBuffer> callback)
        {
            byte[] buffer = new byte[UdpProtocol.BUFFER_SIZE];
            PackageContainer packageReceiver = new PackageContainer();
            socket.BeginReceiveFrom(buffer, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint, ReceiveCallback, new AsyncTripletState<Socket, byte[], PackageContainer>() { State = socket, Data = buffer, CallBack = callback, Complement = packageReceiver });
        }

        /// <summary>
        /// Use this function to receive data from the network asynchronously.
        /// </summary>
        /// <returns>Returns an INetworkBuffer object with data received.</returns>
        public async Task<INetworkBuffer> ReceiveAsync()
        {
            byte[] buffer = new byte[UdpProtocol.BUFFER_SIZE];
            var task = new TaskCompletionSource<INetworkBuffer>();
            PackageContainer packageReceiver = new PackageContainer();
            socket.BeginReceiveFrom(buffer, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint, ReceiveCallback, new AsyncTripletState<Socket, byte[], PackageContainer>() { State = socket, Data = buffer, CallBack = task, Complement = packageReceiver });
            return await task.Task;
        }

        /// <summary>
        /// Use this function to receive data from the network. This function blocks until data is received.
        /// </summary>
        /// <returns>Returns a Network Buffer with the data received.</returns>
        public INetworkBuffer Receive()
        {
            PackageContainer packageReceiver = new PackageContainer();
            while (!packageReceiver.IsComplete())
            {
                byte[] buffer = new byte[UdpProtocol.BUFFER_SIZE];
                int size = socket.ReceiveFrom(buffer, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint);
                if (size > 0)
                {
                    var _copy = new byte[size];
                    Array.Copy(buffer, 0, _copy, 0, size);
                    if (!packageReceiver.Parse(_copy))
                    {
                        //We received a new package while the previous one was incomplete. For now, the behavior is to discard the old package and continue with the new.
                        packageReceiver = new PackageContainer();
                        packageReceiver.Parse(_copy);
                    }
                }
            }
            if (packageReceiver.IsComplete())
                return packageReceiver.ToBuffer();
            return null;
        }

        #endregion

        #endregion

        #region Private Functions

        private void ReceiveCallback(IAsyncResult ar)
        {
            AsyncTripletState<Socket, byte[], PackageContainer> state = ar.AsyncState as AsyncTripletState<Socket, byte[], PackageContainer>;
            try
            {
                int size = state.State.EndReceiveFrom(ar,ref endPoint);
                byte[] received = null;
                if (size > 0)
                {
                    received = new byte[size];
                    Array.Copy(state.Data, 0, received, 0, size);
                    state.Complement.Parse(received);
                }
                if (state.Complement.IsComplete())
                {
                    if (state.CallBack != null && state.CallBack is Action<INetworkBuffer>)
                        (state.CallBack as Action<INetworkBuffer>).Invoke(state.Complement.ToBuffer());
                    else if (state.CallBack != null & state.CallBack is TaskCompletionSource<INetworkBuffer>)
                        (state.CallBack as TaskCompletionSource<INetworkBuffer>).SetResult(state.Complement.ToBuffer());
                }
                else
                    state.State.BeginReceiveFrom(state.Data, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint, ReceiveCallback, state);
            }
            catch (Exception ex)
            {
                if (state.CallBack != null && state.CallBack is TaskCompletionSource<int>)
                    (state.CallBack as TaskCompletionSource<int>).SetException(ex);
                else
                    throw ex;
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            AsyncTripletState<Socket, Package, int> state = ar.AsyncState as AsyncTripletState<Socket, Package, int>;
            try
            {
                int sent = state.State.EndSendTo(ar);
                if (sent > 0)
                {
                    state.Complement += state.Data.Data?.Length??0;
                }
                if (state.Data.Next == null)
                {
                    if (state.CallBack != null && state.CallBack is Action<int>)
                        (state.CallBack as Action<int>).Invoke(state.Complement);
                    else if (state.CallBack != null && state.CallBack is TaskCompletionSource<int>)
                        (state.CallBack as TaskCompletionSource<int>).SetResult(state.Complement);
                }
                else
                {
                    var package = state.Data.Next.Pack();
                    state.Data = state.Data.Next;
                    state.State.BeginSendTo(package, 0, package.Length, 0, endPoint, SendCallback, state);
                }
            }
            catch (Exception ex)
            {
                if (state.CallBack != null && state.CallBack is TaskCompletionSource<int>)
                    (state.CallBack as TaskCompletionSource<int>).SetException(ex);
                else
                    throw ex;
            }
        }

        #endregion

    }
}

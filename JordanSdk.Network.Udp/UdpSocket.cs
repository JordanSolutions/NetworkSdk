using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using JordanSdk.Network.Core;
using System.Net;

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

        public bool Connected { get { return connected; } internal set { connected = value; } }

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

            TaskCompletionSource<bool> task = new TaskCompletionSource<bool>();
            socket.Shutdown(SocketShutdown.Both);
            var iasyncResult = socket.BeginDisconnect(false, (e) =>
            {
                try
                {
                    socket.EndDisconnect(e);
                    socket.Close();
                    Connected = false;
                    task.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    task.SetException(ex);
                }
                finally
                {
                    OnSocketDisconnected?.Invoke(this);
                }
            }, this);
            await task.Task;
        }

        public void DisconnectAsync(Action callback)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.BeginDisconnect(false, (e) =>
            {
                try
                {
                    socket.EndDisconnect(e);
                    socket.Close();
                    Connected = false;
                    callback?.Invoke();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    OnSocketDisconnected?.Invoke(this);
                }
            }, this);
        }

        #endregion

        #region Sending

        public int Send(INetworkBuffer data)
        {
            int bytesSent = 0;
            var clone = data.Size > UdpProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            byte[] _data = clone.Read(UdpProtocol.BUFFER_SIZE);
            while (_data != null)
            {
                bytesSent += socket.SendTo(_data, endPoint);
                _data = clone.Read(UdpProtocol.BUFFER_SIZE);
            }
            return bytesSent;
        }

        public async Task<int> SendAsync(INetworkBuffer data)
        {
            TaskCompletionSource<int> task = new TaskCompletionSource<int>();
            //Need to create an immutable copy if the total count of bytes is greater than buffer size.
            var clone = data.Size > UdpProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            byte[] _data = clone.Read(UdpProtocol.BUFFER_SIZE);
            var iresult = socket.BeginSendTo(_data, 0, _data.Length, 0,endPoint, SendCallback, new AsyncTripletState<Socket, INetworkBuffer, int>() { State = socket, Data = clone, Complement = 0, CallBack = task });
            return await task.Task;
        }

        public void SendAsync(INetworkBuffer data, Action<int> callback)
        {
            //Need to create an immutable copy if the total count of bytes is greater than buffer size.
            var clone = data.Size > UdpProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            byte[] _data = clone.Read(UdpProtocol.BUFFER_SIZE);
            socket.BeginSendTo(_data, 0, _data.Length, 0,endPoint, SendCallback, new AsyncTripletState<Socket, INetworkBuffer, int>() { State = socket, Data = clone, Complement = 0, CallBack = callback });
        }

        #endregion

        #region Receiving Data

        public void ReceiveAsync(Action<INetworkBuffer> callback)
        {
            byte[] buffer = new byte[UdpProtocol.BUFFER_SIZE];
            socket.BeginReceiveFrom(buffer, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint, ReceiveCallback, new AsyncTupleState<Socket, byte[]>() { State = socket, Data = buffer, CallBack = callback });
        }

        public async Task<INetworkBuffer> ReceiveAsync()
        {
            byte[] buffer = new byte[UdpProtocol.BUFFER_SIZE];
            var task = new TaskCompletionSource<INetworkBuffer>();
            socket.BeginReceiveFrom(buffer, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint, ReceiveCallback, new AsyncTupleState<Socket, byte[]>() { State = socket, Data = buffer, CallBack = task });
            return await task.Task;
        }

        public INetworkBuffer Receive()
        {
            byte[] buffer = new byte[UdpProtocol.BUFFER_SIZE];

            int size = socket.ReceiveFrom(buffer, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint);
            NetworkBuffer result = new NetworkBuffer(size);
            if (size > 0)
                result.AppendConstrained(buffer, 0, (uint)size);
            return result;
        }

        #endregion

        #endregion

        #region Private Functions

        private void ReceiveCallback(IAsyncResult ar)
        {
            AsyncTupleState<Socket, byte[]> state = ar.AsyncState as AsyncTupleState<Socket, byte[]>;
            try
            {
                int size = state.State.EndReceiveFrom(ar,ref endPoint);
                NetworkBuffer result = new NetworkBuffer(size);
                if (size > 0)
                    result.AppendConstrained(state.Data, 0, (uint)size);

                if (state.CallBack != null && state.CallBack is Action<INetworkBuffer>)
                    (state.CallBack as Action<INetworkBuffer>).Invoke(result);
                else if (state.CallBack != null & state.CallBack is TaskCompletionSource<INetworkBuffer>)
                    (state.CallBack as TaskCompletionSource<INetworkBuffer>).SetResult(result);
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
            AsyncTripletState<Socket, INetworkBuffer, int> state = ar.AsyncState as AsyncTripletState<Socket, INetworkBuffer, int>;
            try
            {
                int sent = state.State.EndSendTo(ar);
                if (sent > 0)
                    state.Complement += sent;
                var remaining = state.Data.Read(UdpProtocol.BUFFER_SIZE);

                if (remaining == null)
                {
                    if (state.CallBack != null && state.CallBack is Action<int>)
                        (state.CallBack as Action<int>).Invoke(state.Complement);
                    else if (state.CallBack != null && state.CallBack is TaskCompletionSource<int>)
                        (state.CallBack as TaskCompletionSource<int>).SetResult(state.Complement);
                }
                else
                    state.State.BeginSend(remaining, 0, remaining.Length, 0, SendCallback, state);
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

using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using JordanSdk.Network.Core;

namespace JordanSdk.Network.Tcp
{
    public class TcpSocket : ISocket
    {
        #region Private Fields
        Socket socket;
        string token;
        #endregion

        #region Events
        /// <summary>
        /// Event invoked when the connection is lost or purposely closed.
        /// </summary>
        public event DisconnectedDelegate OnSocketDisconnected;

        #endregion

        #region Public Properties

        public bool Connected { get { return socket?.Connected ?? false; } }

        public string Token { get { return token; } }

        #endregion

        #region Constructor

        internal TcpSocket(Socket socket, string token)
        {
            if (socket == null)
                throw new ArgumentNullException("socket", "Socket can not be null.");
            this.socket = socket;
            this.token = token;
        }



        #endregion

        #region ISocket

        #region Connection Management

        /// <summary>
        /// Disconnects the socket blocking until the operation completes.
        /// </summary>
        public void Disconnect()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(false);
            socket.Close();
            OnSocketDisconnected?.Invoke(this);
        }

        /// <summary>
        /// Disconnects the socket asynchronously.
        /// </summary>
        /// <returns>Returns a Task that can be used to wait for the operation to complete.</returns>
        public async Task DisconnectAsync()
        {

            TaskCompletionSource<bool> task = new TaskCompletionSource<bool>();
            socket.Shutdown(SocketShutdown.Both);
            socket.BeginDisconnect(false, (e) =>
            {
                try
                {
                    socket.EndDisconnect(e);
                    socket.Close();
                    task.TrySetResult(true);
                }
                catch (Exception ex) {
                    task.SetException(ex);
                }
                finally
                {
                    OnSocketDisconnected?.Invoke(this);
                }
            }, this);
            await task.Task;
        }

        /// <summary>
        /// Use this function to disconnect the socket asynchronously. Once the operation succeeds, the provided callback will be invoked.
        /// </summary>
        /// <param name="callback">Callback invoked when the socket is disconnected.</param>
        public void DisconnectAsync(Action callback)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.BeginDisconnect(false, (e) =>
            {
                try
                {
                    socket.EndDisconnect(e);
                    socket.Close();
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

        /// <summary>
        /// Use this function to send a buffer over the network. This method blocks until all data in buffer is sent.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes sent.</returns>
        public int Send(INetworkBuffer data)
        {
            int bytesSent = 0;
            var clone = data.Size > TcpProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            byte[] _data = clone.Read(TcpProtocol.BUFFER_SIZE);
            while (_data != null)
            {
                bytesSent += socket.Send(_data);
                _data = clone.Read(TcpProtocol.BUFFER_SIZE);
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
            var clone = data.Size > TcpProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            byte[]  _data = clone.Read(TcpProtocol.BUFFER_SIZE);
            var iresult = socket.BeginSend(_data, 0, _data.Length, 0, SendCallback, new AsyncTripletState<Socket, INetworkBuffer, int>() { State = socket, Data = clone, Complement = 0, CallBack = task });
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
            var clone = data.Size > TcpProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            byte[] _data = clone.Read(TcpProtocol.BUFFER_SIZE);
            socket.BeginSend(_data, 0, _data.Length, 0, SendCallback, new AsyncTripletState<Socket, INetworkBuffer, int>() { State = socket, Data = clone, Complement = 0, CallBack = callback });
        }

        #endregion

        #region Receiving Data

        /// <summary>
        /// Use this function to receive data from the network asynchronously. This function will invoke the provided action once data is received.
        /// </summary>
        /// <param name="callback">Callback to be invoked when data is received.</param>
        public void ReceiveAsync(Action<INetworkBuffer> callback)
        {
            byte[] buffer = new byte[TcpProtocol.BUFFER_SIZE];
            socket.BeginReceive(buffer, 0, TcpProtocol.BUFFER_SIZE, 0, ReceiveCallback, new AsyncTupleState<Socket, byte[]>() { State = socket, Data = buffer, CallBack = callback });
        }

        /// <summary>
        /// Use this function to receive data from the network asynchronously.
        /// </summary>
        /// <returns>Returns an INetworkBuffer object with data received.</returns>
        public async Task<INetworkBuffer> ReceiveAsync()
        {
            byte[] buffer = new byte[TcpProtocol.BUFFER_SIZE];
            var task = new TaskCompletionSource<INetworkBuffer>();
            socket.BeginReceive(buffer, 0, TcpProtocol.BUFFER_SIZE, 0, ReceiveCallback, new AsyncTupleState<Socket, byte[]>() { State = socket, Data = buffer, CallBack = task });
            return await task.Task;
        }

        /// <summary>
        /// Use this function to receive data from the network. This function blocks until data is received.
        /// </summary>
        /// <returns>Returns a Network Buffer with the data received.</returns>
        public INetworkBuffer Receive()
        {
            byte[] buffer = new byte[TcpProtocol.BUFFER_SIZE];
            int size = socket.Receive(buffer, 0, TcpProtocol.BUFFER_SIZE, 0);
            NetworkBuffer result = new NetworkBuffer(size);
            if (size > 0)
                result.AppendConstrained(buffer, 0, (uint)size);
            return result;
        }

        #endregion

        #endregion

        #region Private Functions

        private static void ReceiveCallback(IAsyncResult ar)
        {
            AsyncTupleState<Socket, byte[]> state = ar.AsyncState as AsyncTupleState<Socket, byte[]>;
            try
            {
                int size = state.State.EndReceive(ar);
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

        private static void SendCallback(IAsyncResult ar)
        {
            AsyncTripletState<Socket, INetworkBuffer, int> state = ar.AsyncState as AsyncTripletState<Socket, INetworkBuffer, int>;
            try
            {
                int sent = state.State.EndSend(ar);
                if (sent > 0)
                    state.Complement += sent;
                var remaining = state.Data.Read(TcpProtocol.BUFFER_SIZE);

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

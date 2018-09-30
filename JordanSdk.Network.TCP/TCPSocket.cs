using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using JordanSdk.Network.Core;
using System.Threading;
using System.IO;

namespace JordanSdk.Network.TCP
{
    public class TCPSocket : ISocket
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

        internal TCPSocket(Socket socket, string token)
        {
            if (socket == null)
                throw new ArgumentNullException("socket", "Socket can not be null.");
            this.socket = socket;
            this.token = token;
        }



        #endregion

        #region ISocket

        #region Connection Management

        public void Disconnect()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(false);
            socket.Close();
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

        public int Send(INetworkBuffer data)
        {
            int bytesSent = 0;
            var clone = data.Size > TCPProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            byte[] _data = clone.Read(TCPProtocol.BUFFER_SIZE);
            while (_data != null)
            {
                bytesSent += socket.Send(_data);
                _data = clone.Read(TCPProtocol.BUFFER_SIZE);
            }
            return bytesSent;
        }

        public async Task<int> SendAsync(INetworkBuffer data)
        {
            TaskCompletionSource<int> task = new TaskCompletionSource<int>();
            //Need to create an immutable copy if the total count of bytes is greater than buffer size.
            var clone = data.Size > TCPProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            byte[]  _data = clone.Read(TCPProtocol.BUFFER_SIZE);
            var iresult = socket.BeginSend(_data, 0, _data.Length, 0, SendCallback, new GenericInOutAsyncState<INetworkBuffer, int>() { In = clone, Out = 0, CallBack = task, Socket = socket });
            return await task.Task;
        }

        public void SendAsync(INetworkBuffer data, Action<int> callback)
        {
            //Need to create an immutable copy if the total count of bytes is greater than buffer size.
            var clone = data.Size > TCPProtocol.BUFFER_SIZE ? data.Clone() : data;
            clone.ResetPosition();
            byte[] _data = clone.Read(TCPProtocol.BUFFER_SIZE);
            socket.BeginSend(_data, 0, _data.Length, 0, SendCallback, new GenericInOutAsyncState<INetworkBuffer, int>() { In = clone, Out = 0, CallBack = callback, Socket = socket });
        }

        #endregion

        #region Receiving Data

        public void ReceiveAsync(Action<INetworkBuffer> callback)
        {
            byte[] buffer = new byte[TCPProtocol.BUFFER_SIZE];
            socket.BeginReceive(buffer, 0, TCPProtocol.BUFFER_SIZE, 0, ReceiveCallback, new GenericAsyncState<byte[]>() { Data = buffer, CallBack = callback, Socket = socket });
        }

        public async Task<INetworkBuffer> ReceiveAsync()
        {
            byte[] buffer = new byte[TCPProtocol.BUFFER_SIZE];
            var task = new TaskCompletionSource<INetworkBuffer>();
            socket.BeginReceive(buffer, 0, TCPProtocol.BUFFER_SIZE, 0, ReceiveCallback, new GenericAsyncState<byte[]>() { Data = buffer, CallBack = task, Socket = socket });
            return await task.Task;
        }

        public INetworkBuffer Receive()
        {
            byte[] buffer = new byte[TCPProtocol.BUFFER_SIZE];
            int size = socket.Receive(buffer, 0, TCPProtocol.BUFFER_SIZE, 0);
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
            GenericAsyncState<byte[]> state = ar.AsyncState as GenericAsyncState<byte[]>;
            try
            {
                int size = state.Socket.EndReceive(ar);
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
            GenericInOutAsyncState<INetworkBuffer, int> state = ar.AsyncState as GenericInOutAsyncState<INetworkBuffer, int>;
            try
            {
                int sent = state.Socket.EndSend(ar);
                if (sent > 0)
                    state.Out += sent;
                var remaining = state.In.Read(TCPProtocol.BUFFER_SIZE);

                if (remaining == null)
                {
                    if (state.CallBack != null && state.CallBack is Action<int>)
                        (state.CallBack as Action<int>).Invoke(state.Out);
                    else if (state.CallBack != null && state.CallBack is TaskCompletionSource<int>)
                        (state.CallBack as TaskCompletionSource<int>).SetResult(state.Out);
                }
                else
                    state.Socket.BeginSend(remaining, 0, remaining.Length, 0, SendCallback, state);
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

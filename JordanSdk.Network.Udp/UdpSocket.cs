using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using JordanSdk.Network.Core;
using System.Net;
using System.Linq;

namespace JordanSdk.Network.Udp
{
    /// <summary>
    /// Simplifies sending and receiving data through the network for UDP connectionless sockets.
    /// </summary>
    public class UdpSocket : ISocket
    {
        #region Private Fields
        Socket socket;
        EndPoint endPoint;
        RandomId id;
        bool connected = false;

        #endregion

        #region Events

        /// <summary>
        /// Event invoked when the connection is lost or purposely closed.
        /// </summary>
        public event DisconnectedDelegate OnSocketDisconnected;

        #endregion

        #region Properties

        /// <summary>
        /// This property indicates the connected state of your socket.
        /// </summary>
        public bool Connected { get { return connected; } internal set { connected = value; } }

        /// <summary>
        /// Unique identifier assigned by the server.
        /// </summary>
        public RandomId Id { get { return id; } }

        internal EndPoint RemoteEndPoint => endPoint;

        #endregion

        #region Constructor

        internal UdpSocket(Socket socket, IPEndPoint endPoint, RandomId id)
        {
            if (socket == null)
                throw new ArgumentNullException("socket", "Socket can not be null.");
            else if (endPoint == null)
                throw new ArgumentNullException("endPoint", "Client endpoint can not be null.");
            this.socket = socket;
            this.id = id;
            this.endPoint = endPoint;
        }



        #endregion

        #region ISocket

        #region Connection Management

        /// <summary>
        /// Disconnects the socket blocking until the operation completes.
        /// </summary>
        public void Disconnect()
        {
            Connected = false;
            var _socket = socket;
            socket = null;
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
            _socket?.Dispose();
            
            
            OnSocketDisconnected?.Invoke(this);
        }

        /// <summary>
        /// Disconnects the socket asynchronously.
        /// </summary>
        /// <returns>Returns a Task that can be used to wait for the operation to complete.</returns>
        public async Task DisconnectAsync()
        {
            Connected = false;
            await Task.Run(() =>
            {
                var _socket = socket;
                socket = null;
                _socket?.Shutdown(SocketShutdown.Both);
                _socket?.Close();
                _socket?.Dispose();
                OnSocketDisconnected?.Invoke(this);
                OnSocketDisconnected?.Invoke(this);
            });
        }

        /// <summary>
        /// Use this function to disconnect the socket asynchronously. Once the operation succeeds, the provided callback will be invoked.
        /// </summary>
        /// <param name="callback">Callback invoked when the socket is disconnected.</param>
        public void DisconnectAsync(Action callback)
        {
            Connected = false;
            Task.Run(() =>
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                OnSocketDisconnected?.Invoke(this);
                callback?.Invoke();
            });
        }

        #endregion

        #region Sending

        /// <summary>
        /// Use this function to send data over the network. This method blocks until all data is sent.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes sent.</returns>
        public int Send(byte[] data)
        {
            if (!Connected)
                return 0;
            return socket.SendTo(data, endPoint);
            
        }

        /// <summary>
        /// Use this function to send data over the network asynchronously.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes written to the network.</returns>
        public async Task<int> SendAsync(byte[] data)
        {
            if (!Connected)
                return 0;

            TaskCompletionSource<int> task = new TaskCompletionSource<int>();
            //Need to create an immutable copy if the total count of bytes is greater than buffer size.
            var iresult = socket.BeginSendTo(data, 0, data.Length, 0, endPoint, SendCallback, new AsyncCallbackState<int>() { Socket = socket, Callback = (sent) => {
                task.SetResult(sent);
            } });
            return await task.Task;
        }

        /// <summary>
        /// Use this function to send data over the network asynchronously. This method will invoke the provided action once the operation completes in order to provide feedback.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <param name="callback">Callback invoked once the write operation concludes, containing the amount of bytes sent through the network.</param>
        public void SendAsync(byte[] data, Action<int> callback)
        {
            if (!Connected)
                callback?.Invoke(0);
            socket.BeginSendTo(data, 0, data.Length, 0, endPoint, SendCallback, new AsyncCallbackState<int>() { Socket = socket, Callback = callback });
        }

        #endregion

        #region Receiving Data

        /// <summary>
        /// Use this function to receive data from the network asynchronously. This function will invoke the provided action once data is received.
        /// </summary>
        /// <param name="callback">Callback to be invoked when data is received.</param>
        public void ReceiveAsync(Action<byte[]> callback)
        {
            if (!Connected)
                callback?.Invoke(null);
            byte[] buffer = new byte[UdpProtocol.BUFFER_SIZE];
            socket.BeginReceiveFrom(buffer, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint, ReceiveCallback, new AsyncDataState<byte[], byte[]>() { Socket = socket, Data = buffer, Callback = callback });
        }

        /// <summary>
        /// Use this function to receive data from the network asynchronously.
        /// </summary>
        /// <returns>Returns an awaitable Task with a byte array with data received.</returns>
        public async Task<byte[]> ReceiveAsync()
        {
            if (!Connected)
                return null;
            byte[] buffer = new byte[UdpProtocol.BUFFER_SIZE];
            var task = new TaskCompletionSource<byte[]>();
            socket.BeginReceiveFrom(buffer, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint, ReceiveCallback, new AsyncDataState<byte[], byte[]>() { Socket = socket, Data = buffer, Callback = (result)=> { task.SetResult(result); } });
            return await task.Task;
        }

        /// <summary>
        /// Use this function to receive data from the network. This function blocks until data is received.
        /// </summary>
        /// <returns>Returns a byte array with the data received.</returns>
        public byte[] Receive()
        {
            if (!Connected)
                return null;
            byte[] buffer = new byte[UdpProtocol.BUFFER_SIZE];
            int size = socket.ReceiveFrom(buffer, 0, UdpProtocol.BUFFER_SIZE, 0, ref endPoint);
            if (size > 0)
            {
                var _copy = new byte[size];
                Array.Copy(buffer, 0, _copy, 0, size);
                return _copy;
            }
            return null;
        }

        #endregion

        #endregion

        #region Private Functions

        private void ReceiveCallback(IAsyncResult ar)
        {
            AsyncDataState<byte[], byte[]> state = ar.AsyncState as AsyncDataState<byte[], byte[]>;
            try
            {
                int size = state.Socket.EndReceiveFrom(ar,ref endPoint);
                byte[] received = null;
                if (size > 0)
                {
                    received = new byte[size];
                    Array.Copy(state.Data, 0, received, 0, size);
                }
                state.Callback?.Invoke(received);
            }
            catch (Exception ex)
            {
                state.Callback?.Invoke(null);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            AsyncCallbackState<int> state = ar.AsyncState as AsyncCallbackState<int>;
            try
            {
                int sent = state.Socket.EndSendTo(ar);
                state.Callback?.Invoke(sent);
            }
            catch (Exception ex)
            {
                state.Callback?.Invoke(0);
            }
        }

        #endregion

    }
}

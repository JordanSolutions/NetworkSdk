using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using JordanSdk.Network.Core;

namespace JordanSdk.Network.Tcp
{
    /// <summary>
    /// Simplifies sending and receiving data through the network for TCP connection oriented sockets.
    /// </summary>
    public class TcpSocket : ISocket
    {
        #region Private Fields

        Socket socket;
        RandomId id;
        
        #endregion

        #region Events

        /// <summary>
        /// Event invoked when the connection is lost or closed.
        /// </summary>
        public event DisconnectedDelegate OnSocketDisconnected;

        #endregion

        #region Public Properties

        /// <summary>
        /// True if the socket is connected, false otherwise.
        /// </summary>
        public bool Connected { get { return socket?.Connected ?? false; } }

        /// <summary>
        /// Unique identifier assigned by the server.
        /// </summary>
        public RandomId Id { get { return id; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor taking the underlying socket and unique identifier assigned / created by the server.
        /// </summary>
        /// <param name="socket">Underlying socket object</param>
        /// <param name="id">Unique identifier.</param>
        internal TcpSocket(Socket socket, RandomId id)
        {
            this.socket = socket ?? throw new ArgumentNullException("socket", "Socket can not be null.");
            this.id = id;
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
                    socket.Shutdown(SocketShutdown.Both);
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
                    socket.Shutdown(SocketShutdown.Both);
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
        /// Use this function to send data over the network. This method blocks until all data is sent.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes sent.</returns>
        public int Send(byte[] data)
        {
            return socket.Send(data);
        }

        /// <summary>
        /// Use this function to send data over the network asynchronously.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes written to the network.</returns>
        public async Task<int> SendAsync(byte[] data)
        {
            TaskCompletionSource<int> task = new TaskCompletionSource<int>();
            //Need to create an immutable copy if the total count of bytes is greater than buffer size.
            socket.BeginSend(data, 0, data.Length, 0, SendCallback,
                new AsyncCallbackState<int>()
                {
                    Socket = socket,
                    Callback = (sent) => { task.SetResult(sent); }
                });
            return await task.Task;
        }

        /// <summary>
        /// Use this function to send data over the network asynchronously. This method will invoke the provided action once the operation completes in order to provide feedback.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <param name="callback">Callback invoked once the write operation concludes, containing the amount of bytes sent through the network.</param>
        public void SendAsync(byte[] data, Action<int> callback)
        {
            //Need to create an immutable copy if the total count of bytes is greater than buffer size.
            socket.BeginSend(data, 0, data.Length, 0, SendCallback, new AsyncCallbackState<int>() { Socket = socket, Callback = callback });
        }

        #endregion

        #region Receiving

        /// <summary>
        /// Use this function to receive data from the network asynchronously. This function will invoke the provided action once data is received.
        /// </summary>
        /// <param name="callback">Callback to be invoked when data is received.</param>
        public void ReceiveAsync(Action<byte[]> callback)
        {
            byte[] buffer = new byte[TcpProtocol.BUFFER_SIZE];
            socket.BeginReceive(buffer, 0, TcpProtocol.BUFFER_SIZE, 0, ReceiveCallback, new AsyncDataState<byte[], byte[]>() { Socket = socket, Data = buffer, Callback = callback });
        }

        /// <summary>
        /// Use this function to receive data from the network asynchronously.
        /// </summary>
        /// <returns>Returns an INetworkBuffer object with data received.</returns>
        public async Task<byte[]> ReceiveAsync()
        {
            byte[] buffer = new byte[TcpProtocol.BUFFER_SIZE];
            var task = new TaskCompletionSource<byte[]>();
            socket.BeginReceive(buffer, 0, TcpProtocol.BUFFER_SIZE, 0, ReceiveCallback, new AsyncDataState<byte[], byte[]>() { Socket = socket, Data = buffer, Callback = (result) => { task.SetResult(result); } });
            return await task.Task;
        }

        /// <summary>
        /// Use this function to receive data from the network. This function blocks until data is received.
        /// </summary>
        /// <returns>Returns a Network Buffer with the data received.</returns>
        public byte[] Receive()
        {
            byte[] buffer = new byte[TcpProtocol.BUFFER_SIZE];
            int size = socket.Receive(buffer, 0, TcpProtocol.BUFFER_SIZE, 0);
            if (size > 0)
            {
                var result = new byte[size];
                Array.Copy(buffer, 0, result, 0, size);
                return result;
            }
            return null;
        }

        #endregion

        #endregion

        #region Private Functions

        private static void ReceiveCallback(IAsyncResult ar)
        {
            AsyncDataState<byte[], byte[]> state = ar.AsyncState as AsyncDataState<byte[], byte[]>;
            try
            {
                int size = state.Socket.EndReceive(ar);
                byte[] result = null;
                if (size > 0)
                {
                    result = new byte[size];
                    Array.Copy(state.Data, 0, result, 0, size);
                }
                state.Callback?.Invoke(result);
            }
            catch (Exception ex)
            {
                state.Callback?.Invoke(null);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            AsyncCallbackState<int> state = ar.AsyncState as AsyncCallbackState<int>;
            try
            {
                int sent = state.Socket.EndSend(ar);
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

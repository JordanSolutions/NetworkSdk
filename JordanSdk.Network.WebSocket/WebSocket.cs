using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using JordanSdk.Network.Core;



namespace JordanSdk.Network.WebSocket
{
    /// <summary>
    /// Simplifies sending and receiving data through the network using the web socket protocol.
    /// </summary>
    public class WebSocket : ISocket
    {
        #region Private Fields

        private System.Net.WebSockets.WebSocket socket;
        CancellationTokenSource connectionManager = new CancellationTokenSource();
        private ConcurrentQueue<byte[]> receivedPackages = new ConcurrentQueue<byte[]>();
        private RandomId id;

        #endregion

        #region Public Properties

        /// <summary>
        /// Unique identifier assigned by the server.
        /// </summary>
        public RandomId Id => id;

        /// <summary>
        /// This property indicates the connected state of the socket.
        /// </summary>
        public bool Connected { get { return socket != null && (socket.State == WebSocketState.Open || socket.State == WebSocketState.Connecting); } }

        #endregion

        #region Events

        /// <summary>
        /// Event invoked when the connection is lost or closed.
        /// </summary>
        public event DisconnectedDelegate OnSocketDisconnected;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor taking the underlying socket and unique identifier assigned / created by the server.
        /// </summary>
        /// <param name="socket">Underlying socket object</param>
        /// <param name="id">Unique identifier.</param>
        internal WebSocket(System.Net.WebSockets.WebSocket socket, RandomId id)
        {
            this.socket = socket ?? throw new ArgumentNullException("socket", "Socket can not be null.");
            this.id = id;
            InternalReceive();
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Disconnects the socket blocking until the operation completes.
        /// </summary>
        public void Disconnect()
        {
            CancellationTokenSource tsource = new CancellationTokenSource(20000);
            Task toAwait = this.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", tsource.Token);
            Task.WaitAll(toAwait);
            connectionManager.Cancel();
            OnSocketDisconnected?.Invoke(this);
        }
        /// <summary>
        /// Disconnects the socket asynchronously.
        /// </summary>
        /// <returns>Returns a Task that can be used to wait for the operation to complete.</returns>
        public async Task DisconnectAsync()
        {
            CancellationTokenSource tsource = new CancellationTokenSource(20000);
            await this.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", tsource.Token);
            connectionManager.Cancel();
            OnSocketDisconnected?.Invoke(this);
        }

        /// <summary>
        /// Use this function to disconnect the socket asynchronously. Once the operation succeeds, the provided callback will be invoked.
        /// </summary>
        /// <param name="callback">Callback invoked when the socket is disconnected.</param>
        public void DisconnectAsync(Action callback)
        {
            Task.Run(async () =>
            {
                CancellationTokenSource tsource = new CancellationTokenSource(20000);
                await this.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", tsource.Token);
                OnSocketDisconnected?.Invoke(this);
                connectionManager.Cancel();
                callback?.Invoke();
            });
        }

        /// <summary>
        /// Use this function to receive data from the network synchronously. This function blocks until data is received or until underlying socket receive time out.
        /// </summary>
        /// <returns>Returns a Network Buffer with the data received.</returns>
        public byte[] Receive()
        {
            while (!connectionManager.IsCancellationRequested) {
                if(receivedPackages.Count > 0)
                {
                    byte[] data;
                    receivedPackages.TryDequeue(out data);
                    return data;
                }
                Task.WaitAll(Task.Delay(200, connectionManager.Token));
            }
            return null;
        }

        /// <summary>
        /// Use this function to receive data from the network asynchronously. This function will invoke the provided action once data is received. 
        /// </summary>
        /// <param name="callback">Callback to be invoked when data is received.</param>
        public void ReceiveAsync(Action<byte[]> callback)
        {
            Task.Run(async () =>
            {
                while (!connectionManager.IsCancellationRequested)
                {
                    if (receivedPackages.Count > 0)
                    {
                        byte[] data;
                        receivedPackages.TryDequeue(out data);
                        callback?.Invoke(data);
                    }
                    await Task.Delay(200, connectionManager.Token);
                }
                callback?.Invoke(null);
            });
        }

        /// <summary>
        /// Use this Task oriented function to send data over the network asynchronously.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes written to the network.</returns>
        public async Task<byte[]> ReceiveAsync()
        {
            while (!connectionManager.IsCancellationRequested)
            {
                if (receivedPackages.Count > 0)
                {
                    byte[] data;
                    receivedPackages.TryDequeue(out data);
                    return data;
                }
                await Task.Delay(200, connectionManager.Token);
            }
            return null;

        }

        /// <summary>
        /// Use this function to send a buffer over the network. This method blocks until all data in buffer is sent or until underlying socket receive time out.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes sent.</returns>
        public int Send(byte[] data)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(data);
            Task.WaitAll(socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None));
            return data.Length; //There is no other way
        }

        /// <summary>
        /// Use this Task oriented function to send data over the network asynchronously.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes written to the network.</returns>
        public async Task<int> SendAsync(byte[] data)
        {
            ArraySegment<byte> buffer = new ArraySegment<byte>(data);
            await socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
            return data.Length; //There is no other way
        }

        /// <summary>
        /// Use this function to send data over the network asynchronously. This method will invoke the provided action once the operation completes.
        /// </summary>
        /// <param name="data">INetworkBuffer containing the data to be sent.</param>
        /// <param name="callback">Callback invoked once the write operation concludes, containing the amount of bytes sent through the network.</param>
        public void SendAsync(byte[] data, Action<int> callback)
        {
            Task.Run(async () =>
            {
                ArraySegment<byte> buffer = new ArraySegment<byte>(data);
                await socket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
                callback?.Invoke(data.Length); //There is no other way
            });
        }

        #endregion

        #region Private Functions

        private void InternalReceive()
        {
            Task.Run(async () =>
            {
                byte[] _buffer = new byte[WebSocketProtocol.BUFFER_SIZE];
                while (Connected && ! connectionManager.Token.IsCancellationRequested)
                {
                    ArraySegment<byte> buffer = new ArraySegment<byte>(_buffer);
                    var received = await socket.ReceiveAsync(buffer, connectionManager.Token);
                    if (received.MessageType != WebSocketMessageType.Close)
                    {
                        byte[] result = new byte[received.Count];
                        Array.ConstrainedCopy(buffer.Array, 0, result, 0, received.Count);
                        receivedPackages.Enqueue(result);
                    }
                    else
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", connectionManager.Token);
                    }
                }
            });
        }

        #endregion
    }
}

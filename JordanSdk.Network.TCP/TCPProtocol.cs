using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using JordanSdk.Network.Core;

namespace JordanSdk.Network.TCP
{
    /// <summary>
    /// This class is the TCP Implementation of IProtocol, and simplifies TCP network management for all possible operations such as listening and accepting incoming connections, connecting as a client to a remote server and much more.
    /// </summary>
    public class TCPProtocol : IProtocol<TCPSocket>, IDisposable
    {
        #region Private Fields

        AcceptConnectionState state = new AcceptConnectionState();
        private ConcurrentDictionary<string, ISocket> csockets = new ConcurrentDictionary<string, ISocket>();
        CancellationTokenSource listenerManager = null;

        #endregion

        #region Constants

        /// <summary>
        /// Internal constant used for optimizing read/write network buffer size.
        /// </summary>
        internal const int BUFFER_SIZE = 8192;

        /// <summary>
        /// Internal constant used for specifying the amount of time in milliseconds a send operation times out. (set to one minute)
        /// </summary>
        internal const int SEND_TIMEOUT = 60000;

        /// <summary>
        /// Internal constant used for specifying the amount of time in milliseconds a receive operation times out. (set to one minute)
        /// </summary>
        internal const int RECEIVE_TIMEOUT = 60000;


        #endregion

        #region Public Properties

        /// <summary>
        /// True when listening for incoming connections.
        /// </summary>
        public bool Listening { get; private set; } = false;

        /// <summary>
        /// This property specifies the kind of IP version to be used for listening.
        /// </summary>
        public IPAddressKind IPAddressKind { get; set; } = IPAddressKind.IPV4;


        /// <summary>
        /// Use this property to specify the port to start listening on.
        /// </summary>
        public int Port { get; set; }


        /// <summary>
        ///This property is used for connecting to a remote server, or restrict the IP address (TCP-UDP)/URL(WebSockets - Others) the server will bind to.
        /// </summary>
        public string Address { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// This event is invoked when a client attempts to establish a socket connection in order to give an opportunity to the protocol owner to accept or reject the connection.
        /// </summary>
        public event UserConnectedDelegate OnConnectionRequested;

        #endregion

        #region Public Functions

        /// <summary>
        /// Starts listening for incoming connection.
        /// </summary>
        public void Listen()
        {
            if (IPAddressKind == IPAddressKind.IPV4)
                SetupIPV4();
            else
                SetupIPV6();

            StartListening();
        }

        /// <summary>
        /// Stops listening for incoming connections.
        /// </summary>
        public void StopListening()
        {
            if(listenerManager != null && !listenerManager.IsCancellationRequested)
            {
                Listening = false;
                ReleaseClients();
                state.ResetEvent.Reset(); //Releases the thread
                try
                {
                    listenerManager.Cancel(false);
                }
                catch (Exception) { }
                {
                    
                }
                state.Listener?.Close();
                state.Listener?.Dispose();
                state.Listener = null;
            }
        }


        /// <summary>
        /// Initiates an asynchronous connection to a remote server.
        /// </summary>
        /// <returns>Returns an instance of TCP Socket</returns>
        public async Task<TCPSocket> ConnectAsync()
        {
            var task = new TaskCompletionSource<TCPSocket>();
            var endPoint = new IPEndPoint(IPAddress.Parse(Address ?? "127.0.0.1"), Port);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(endPoint, AsyncConnectCallback, new AsyncState() { Socket = socket, CallBack = task });
            return await task.Task;
        }

        /// <summary>
        /// Initiates an asynchronous connection to a remote server, calling callback once the connection is established.
        /// </summary>
        /// <returns>Returns an instance of TCP Socket</returns>
        public void ConnectAsync(Action<TCPSocket> callback)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(Address ?? "127.0.0.1"), Port);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(endPoint, AsyncConnectCallback, new AsyncState() { Socket = socket, CallBack = callback });
        }

        /// <summary>
        /// Initiates a synchronous connection to a remote server.
        /// </summary>
        /// <returns>Returns an instance of TCP Socket</returns>
        public TCPSocket Connect()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(Address ?? "127.0.0.1"), Port);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
            return SetupClientToken(socket);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Private Functions

        private void ReleaseClients()
        {
            Parallel.ForEach(csockets.Values,(socket)=>{
                try
                {
                    socket.Disconnect();
                }
                catch (Exception) { } //Ignoring any errors, we are shutting down anyways
            });
        }

        private void SetupIPV4()
        {
            if(string.IsNullOrWhiteSpace(Address))
                state.Endpoint = new IPEndPoint(IPAddress.Any, Port);
            else
                state.Endpoint = new IPEndPoint(IPAddress.Parse(Address), Port);

            state.Listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            SetupCommonListenerFields();
        }

        private void SetupIPV6()
        {
            if(string.IsNullOrWhiteSpace(Address))
                state.Endpoint = new IPEndPoint(IPAddress.IPv6Any, Port);
            else
                state.Endpoint = new IPEndPoint(IPAddress.Parse(Address), Port);

            state.Listener = new Socket(AddressFamily.InterNetworkV6,
               SocketType.Stream, ProtocolType.Tcp);
            state.Listener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            SetupCommonListenerFields();
        }

        private void SetupCommonListenerFields()
        {
            state.Listener.NoDelay = true;
            state.Listener.Ttl = 255;
            state.Listener.SendBufferSize = BUFFER_SIZE;
            state.Listener.ReceiveBufferSize = BUFFER_SIZE;
            state.Listener.LingerState = new LingerOption(true, 10);
            state.Listener.SendTimeout = SEND_TIMEOUT;
            state.Listener.ReceiveTimeout = RECEIVE_TIMEOUT;
            state.Listener.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
        }

        private void StartListening()
        {
            if (Listening)
                return;
            Listening = true;
            state.Listener.Bind(state.Endpoint);
            state.Listener.Listen(2000);
            listenerManager = new CancellationTokenSource();
            var throwAway = Task.Run(() =>
            {
                try
                {
                    while (Listening)
                    {
                        state.Listener.BeginAccept(new AsyncCallback(AcceptConnection),
                            state);
                        state.ResetEvent.Wait();
                        state.ResetEvent.Reset();
                    }
                }
                catch (ThreadAbortException) { }
            }, listenerManager.Token);
        }

        private void AcceptConnection(IAsyncResult ar)
        {

            Socket connected = null;
            AcceptConnectionState state = (AcceptConnectionState)ar.AsyncState;
            try
            {
                connected = state.Listener?.EndAccept(ar);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) {
                Diagnostic.DiagnosticCenter.Instance.Log?.LogException<Exception>(ex);
            }
            finally
            {
                state.ResetEvent.Set();
            }
            try
            {
                if (connected != null)
                    CollectSocket(connected);
            }
            catch (Exception ex)
            {
                Diagnostic.DiagnosticCenter.Instance.Log?.LogException<Exception>(ex);
            }
        }

        private class AcceptConnectionState
        {
            public Socket Listener { get; set; }

            public ManualResetEventSlim ResetEvent { get; set; } = new ManualResetEventSlim(false);

            public IPEndPoint Endpoint { get; set; }
        }

        private void CollectSocket(Socket socket)
        {
            ISocket isocket = new TCPSocket(socket, Guid.NewGuid().ToString("N"));
            while (!csockets.TryAdd(isocket.Token, isocket))
                isocket = new TCPSocket(socket, Guid.NewGuid().ToString("N"));
            isocket.OnSocketDisconnected += RemoveSocket;
            SendServerToken(isocket);
            OnConnectionRequested?.Invoke(isocket);
        }
        private void RemoveSocket(ISocket socket)
        {
            ISocket isocket;
            if(csockets.TryRemove(socket.Token,out isocket))
                isocket.OnSocketDisconnected -= RemoveSocket;
        }

        #region IDisposable Support / Finalizer

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if(disposing && Listening)
                        StopListening();

                listenerManager?.Dispose();
                listenerManager = null;
                disposedValue = true;
            }
        }

        ~TCPProtocol() {
            Dispose(false);
        }

        

        #endregion

        private static TCPSocket SetupClientToken(Socket socket)
        {
            if (socket.Connected)
            {
                byte[] buffer = new byte[BUFFER_SIZE];
                SocketError error;
                int received = socket.Receive(buffer, 0, BUFFER_SIZE, 0, out error);
                if (received > 0)
                {
                    byte[] tokenBytes = new byte[received];
                    Array.ConstrainedCopy(buffer, 0, tokenBytes, 0, received);
                    return new TCPSocket(socket, Encoding.ASCII.GetString(tokenBytes));
                }
                
            }
            return null;
        }

        private static void SendServerToken(ISocket socket)
        {
            byte[] token = Encoding.ASCII.GetBytes(socket.Token);
            using(NetworkBuffer buffer = new NetworkBuffer(token.Length, token))
            {
                socket.Send(buffer);
            }
        }

        private static void AsyncConnectCallback(IAsyncResult ar)
        {
            AsyncState state = ar.AsyncState as AsyncState;
            try
            {
                state.Socket.EndConnect(ar);
                if (state.CallBack != null && state.CallBack is Action<TCPSocket>)
                    (state.CallBack as Action<TCPSocket>).Invoke(SetupClientToken(state.Socket));
                else if (state.CallBack != null && state.CallBack is TaskCompletionSource<TCPSocket>)
                    (state.CallBack as TaskCompletionSource<TCPSocket>).SetResult(SetupClientToken(state.Socket));
            }
            catch (Exception ex)
            {
                if (ar.AsyncState is TaskCompletionSource<TCPSocket>)
                    (ar.AsyncState as TaskCompletionSource<TCPSocket>).SetException(ex);
                else
                    throw ex;
            }
        }

        #endregion
    }
}

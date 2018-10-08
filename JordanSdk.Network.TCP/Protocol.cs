using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using JordanSdk.Network.Core;

namespace JordanSdk.Network.Tcp
{
    /// <summary>
    /// This class is the TCP Implementation of IProtocol, and simplifies connection oriented network management for all possible operations such as listening, accepting incoming connections, and connecting as a client to a remote server.
    /// </summary>
    public class TcpProtocol : IProtocol<TcpSocket>, IDisposable
    {
        #region Private Fields

        AcceptConnectionState state = new AcceptConnectionState();
        private ConcurrentDictionary<RandomId, ISocket> csockets = new ConcurrentDictionary<RandomId, ISocket>();
        #endregion

        #region Constants

        /// <summary>
        /// Internal constant used for optimizing read/write network buffer size.
        /// </summary>
        public const int BUFFER_SIZE = 8192;

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
        /// True when listening for incoming connections, false otherwise.
        /// </summary>
        public bool Listening { get; private set; } = false;

        /// <summary>
        /// Use this property to specify the port to start listening on for incoming connection requests.
        /// </summary>
        public int Port { get; set; }


        /// <summary>
        /// Use this property to specify the IP/Interface to bind to. Defaults to IPV4, Any IP address (0.0.0.0).
        /// Examples: IPV4 Local Host - '127.0.0.1', IPV6 Local Host - '::1'
        /// </summary>
        public string Address { get; set; } = "0.0.0.0";

        #endregion

        #region Events

        /// <summary>
        /// This event is invoked when a client attempts to establish a socket connection in order to give an opportunity to the protocol owner to accept (via not doing anything) or reject (via Disconnect) the connection.
        /// </summary>
        public event SocketConnectedDelegate OnConnectionRequested;

        #endregion

        #region Public Functions

        /// <summary>
        /// Starts listening for incoming connection.
        /// </summary>
        public void Listen()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
            if (endPoint.AddressFamily == AddressFamily.InterNetwork)
                SetupIPV4(endPoint);
            else
                SetupIPV6(endPoint);
            state.Listener.Bind(endPoint);
            StartListening();
        }

        /// <summary>
        /// Stops listening for incoming connections.
        /// </summary>
        public void StopListening()
        {
                Listening = false;
                ReleaseClients();
                state.Listener.Close();
                state.Listener.Dispose();
                state.Listener = null;
        }

        /// <summary>
        /// Initiates an asynchronous connection to a remote server.
        /// </summary>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <returns>Returns an instance of TCP Socket</returns>
        public async Task<TcpSocket> ConnectAsync(string remoteIp, int remotePort)
        {
            var task = new TaskCompletionSource<TcpSocket>();
            var endPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
                 socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            SetupCommonSocketProperties(socket);
            socket.BeginConnect(endPoint, AsyncConnectCallback, new AsyncCallbackState<TcpSocket>() { Socket = socket, Callback = (tcpSocket)=>
            {
                task.SetResult(tcpSocket);
            }
            });
            return await task.Task;
        }

        /// <summary>
        /// Initiates an asynchronous connection to a remote server, calling callback once the connection is established.
        /// </summary>
        /// <param name="callback">Callback invoked once the connection is established.</param>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <returns>Returns an instance of TCP Socket</returns>
        public void ConnectAsync(Action<TcpSocket> callback, string remoteIp, int remotePort)
        {
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
            var socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
                socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            SetupCommonSocketProperties(socket);
            socket.BeginConnect(remoteEndPoint, AsyncConnectCallback, new AsyncCallbackState<TcpSocket>() { Socket = socket, Callback = callback });
        }

        /// <summary>
        /// Initiates a synchronous connection to a remote server.
        /// </summary>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <returns>Returns an instance of TCP Socket if the connection succeeds.</returns>
        public TcpSocket Connect(string remoteIp, int remotePort)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if(endPoint.AddressFamily == AddressFamily.InterNetworkV6)
                 socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            SetupCommonSocketProperties(socket);
            socket.Connect(endPoint);
            return SetupClientToken(socket);
        }

        /// <summary>
        /// Releases allocated resources.
        /// </summary>
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

        private void SetupIPV4(IPEndPoint endPoint)
        {
            state.Listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            SetupCommonSocketProperties(state.Listener);
           
        }

        private void SetupIPV6(IPEndPoint endPoint)
        {
            state.Listener = new Socket(AddressFamily.InterNetworkV6,
               SocketType.Stream, ProtocolType.Tcp);
            state.Listener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            SetupCommonSocketProperties(state.Listener);
        }

        private static void SetupCommonSocketProperties(Socket socket)
        {
            socket.NoDelay = true;
            socket.Ttl = 255;
            socket.SendBufferSize = BUFFER_SIZE;
            socket.ReceiveBufferSize = BUFFER_SIZE;
            socket.LingerState = new LingerOption(true, 10);
            socket.SendTimeout = SEND_TIMEOUT;
            socket.ReceiveTimeout = RECEIVE_TIMEOUT;
            socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
        }

        private void StartListening()
        {
            if (Listening)
                return;
            Listening = true;
            state.Listener.Listen(2000);
            var istate = state.Listener.BeginAccept(new AsyncCallback(AcceptConnection),
                           state);
        }

        private void AcceptConnection(IAsyncResult ar)
        {
            Socket connected = null;
            AcceptConnectionState state = (AcceptConnectionState)ar.AsyncState;
            var _continue = state.Listener?.IsBound;
            if (!_continue.HasValue || !_continue.Value)
                return;
            try
            {
                connected = state.Listener?.EndAccept(ar);
            }
            catch (ObjectDisposedException) {
                Listening = false;
            }
            catch (Exception ex) {
                Diagnostic.DiagnosticCenter.Instance.Log?.LogException<Exception>(ex);
            }
            if (Listening)
            {
                state.Listener.BeginAccept(new AsyncCallback(AcceptConnection),
                      state);
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
        }

        private class AcceptConnectionState
        {
            public Socket Listener { get; set; }
            public IPEndPoint Endpoint { get; set; }
        }

        private void CollectSocket(Socket socket)
        {
            ISocket isocket = new TcpSocket(socket, RandomId.Generate());
            while (!csockets.TryAdd(isocket.Id, isocket))
                isocket = new TcpSocket(socket, RandomId.Generate());
            isocket.OnSocketDisconnected += RemoveSocket;
            SendServerToken(isocket);
            OnConnectionRequested?.Invoke(isocket);
        }
        private void RemoveSocket(ISocket socket)
        {
            ISocket isocket;
            if(csockets.TryRemove(socket.Id,out isocket))
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
                disposedValue = true;
            }
        }

        ~TcpProtocol() {
            Dispose(false);
        }

        

        #endregion

        private static TcpSocket SetupClientToken(Socket socket)
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
                    return new TcpSocket(socket,new RandomId(tokenBytes));
                }
                
            }
            return null;
        }

        private static void SendServerToken(ISocket socket)
        {
            socket.Send(socket.Id.ToArray());
        }

        private static void AsyncConnectCallback(IAsyncResult ar)
        {
            AsyncCallbackState<TcpSocket> asyncState = ar.AsyncState as AsyncCallbackState<TcpSocket>;
            try
            {
                asyncState.Socket.EndConnect(ar);
                asyncState.Callback?.Invoke(SetupClientToken(asyncState.Socket));
            }
            catch (Exception ex)
            {
                asyncState.Callback?.Invoke(null);
            }
        }

        #endregion
    }
}

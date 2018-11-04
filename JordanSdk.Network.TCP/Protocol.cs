using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using JordanSdk.Network.Core;
using Open.Nat;
using System.Threading;

namespace JordanSdk.Network.Tcp
{
    /// <summary>
    /// This class is the TCP Implementation of IProtocol, and simplifies connection oriented network management for all possible operations such as listening, accepting incoming connections, and connecting as a client to a remote server.
    /// </summary>
    public class TcpProtocol : IProtocol<TcpSocket>, IDisposable
    {
        #region Private Fields

        private Socket listener;
        private IPEndPoint _localEndpoint;
        private IPEndPoint _remoteEndpoint;
        private ConcurrentDictionary<RandomId, ISocket> csockets = new ConcurrentDictionary<RandomId, ISocket>();

        private NatDevice nat = null;
        private Mapping portMap = null;
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

        /// <summary>
        /// This property is used for when NAT port mapping / port forwarding is needed. We use Open.Nat which is a great library in order to achieve this. Your implementation needs not to worried about managing port mapping.
        /// </summary>
        public bool EnableNatTraversal { get; set; }

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
        public async void Listen()
        {
            if (Listening)
                return;
            Listening = true;
            var endPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
            listener = SetupListener(endPoint);
            listener.Bind(endPoint);
            if(EnableNatTraversal)
            {
                try
                {
                    await StartNatPortMapping();
                }
                catch (NatDeviceNotFoundException ex)
                {
                    throw ex;
                }
            }
            listener.Listen(2000);
            listener.BeginAccept(new AsyncCallback(AcceptConnection),
                           new AsyncState() { Socket = listener });

        }


        /// <summary>
        /// Stops listening for incoming connections.
        /// </summary>
        public void StopListening()
        {
            if (disposedValue)
                throw new ObjectDisposedException("This protocol object has already been disposed.");
            Listening = false;
            ReleaseClients();
            listener?.Close();
            listener?.Dispose();
            listener = null;
            nat?.DeletePortMapAsync(portMap);
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

        private async Task StartNatPortMapping()
        {
            var disc = new NatDiscoverer();
            nat = await disc.DiscoverDeviceAsync();
            portMap = new Mapping(Protocol.Tcp, Port, Port, "Socket Server Map");
            await nat?.CreatePortMapAsync(portMap);
        }

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

        private static Socket SetupListener(IPEndPoint endPoint)
        {
            Socket result = new Socket(endPoint.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            if(endPoint.AddressFamily == AddressFamily.InterNetworkV6)
                result.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            SetupCommonSocketProperties(result);
            return result;
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

        private void AcceptConnection(IAsyncResult ar)
        {
            Socket connected = null;
            AsyncState state = (AsyncState)ar.AsyncState;
            var _continue = state.Socket?.IsBound;
            if (!_continue.HasValue || !_continue.Value)
                return;
            try
            {
                connected = state.Socket?.EndAccept(ar);
            }
            catch (ObjectDisposedException) {
                Listening = false;
            }
            catch (Exception ex) {
                Diagnostic.DiagnosticCenter.Instance.Log?.LogException<Exception>(ex);
            }
            if (Listening)
            {
                state.Socket.BeginAccept(new AsyncCallback(AcceptConnection),
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

        #region IDisposable Support / Finalizer

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && Listening)
                    StopListening();
                disposedValue = true;
            }
        }

        ~TcpProtocol()
        {
            Dispose(false);
        }



        #endregion
    }
}

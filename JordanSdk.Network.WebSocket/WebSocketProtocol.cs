using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JordanSdk.Network.Core;
using Open.Nat;

namespace JordanSdk.Network.WebSocket
{
    /// <summary>
    /// This class is a web sockets implementation of the iProtocol .
    /// </summary>
    public class WebSocketProtocol : IProtocol
    {
        #region Private Fields

        private bool disposedValue = false; // To detect redundant calls
        private HttpListener httpListener = null;
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
        /// Local port the socket will be bound to. Defaults to 80. 
        /// </summary>
        public int Port { get; set; } = 80;

        /// <summary>
        /// Specifies the URL used to listen to for incoming web socket connections. 
        /// </summary>
        public string Address { get; set; } = "http://localhost/server/";

        /// <summary>
        /// This flag is set to true when the protocol is listening for incoming connections.
        /// </summary>
        public bool Listening { get; private set; }

        #endregion

        #region Public Events

        /// <summary>
        /// Event that is fired when a client connects.
        /// </summary>
        public event SocketConnectedDelegate OnConnectionRequested;

        #endregion

        #region Public Functions

        /// <summary>
        /// Starts listening for incoming connections over 'Address' and 'Port'.
        /// </summary>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        public void Listen(bool enableNatTraversal = false)
        {
            if (Listening)
                return;
            Listening = true;
            httpListener = Setup();
            if (enableNatTraversal)
                StartNatPortMapping();
            httpListener.Start();
            httpListener.BeginGetContext(new AsyncCallback(AcceptConnection), httpListener);
        }

        /// <summary>
        /// Starts listening for incoming connections over 'Address' and 'Port'.
        /// </summary>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        public async Task ListenAsync(bool enableNatTraversal = false)
        {
            if (Listening)
                return;
            Listening = true;
            httpListener = Setup();
            if (enableNatTraversal)
                await StartNatPortMappingAsync();
            httpListener.Start();
            httpListener.BeginGetContext(new AsyncCallback(AcceptConnection), httpListener);
        }

        /// <summary>
        /// Stops listening for incoming connections.
        /// </summary>
        public void StopListening()
        {
            Listening = false;
            ReleaseClients();
            httpListener?.Stop();
            httpListener?.Close();
            httpListener = null;
            nat?.DeletePortMapAsync(portMap);
        }

        /// <summary>
        /// Initiates a client connection.
        /// </summary>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        /// <returns>Returns an instance of the created client ISocket if the connection was established, null otherwise.</returns>
        public ISocket Connect(string remoteIp, int remotePort, bool enableNatTraversal = false)
        {
            if (enableNatTraversal)
                StartNatPortMapping();
            System.Net.WebSockets.ClientWebSocket webSocket = Connect(GetClientAddress(remoteIp, remotePort)).Result;
            return SetupClientToken(webSocket).Result;

        }

        /// <summary>
        /// Initiates a client connection.
        /// </summary>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        /// <returns>Returns an instance of the created client ISocket if the connection was established, null otherwise.</returns>
        public async Task<ISocket> ConnectAsync(string remoteIp, int remotePort, bool enableNatTraversal = false)
        {
           
            if (enableNatTraversal)
                StartNatPortMapping();
            System.Net.WebSockets.ClientWebSocket webSocket = await Connect(GetClientAddress(remoteIp, remotePort));
            return await SetupClientToken(webSocket);
        }

        /// <summary>
        /// Initiates a client connection.
        /// </summary>
        /// <param name="callback">Callback invoked once the connection is established. T will be null if the connection did not succeed.</param>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        /// <returns>Returns an instance of the created client ISocket</returns>
        public void ConnectAsync(Action<ISocket> callback, string remoteIp, int remotePort, bool enableNatTraversal = false)
        {
            Task.Run(async () =>
            {
                if (enableNatTraversal)
                    StartNatPortMapping();
                System.Net.WebSockets.ClientWebSocket webSocket = await Connect(GetClientAddress(remoteIp, remotePort));
                var result = await SetupClientToken(webSocket);
                callback?.Invoke(result);
            });
        }

        /// <summary>
        /// Releases all allocated resources at the same times it attempts to close all connections in a clean manner and stop listening.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }


        #endregion

        #region Private Functions

        private static async Task<WebSocket> SetupClientToken(System.Net.WebSockets.ClientWebSocket socket)
        {
            if (socket.State == System.Net.WebSockets.WebSocketState.Connecting || socket.State == System.Net.WebSockets.WebSocketState.Open)
            {
                byte[] _buffer = new byte[BUFFER_SIZE];

                ArraySegment<byte> buffer = new ArraySegment<byte>(_buffer);
                
                var received = await socket.ReceiveAsync(buffer, CancellationToken.None);
                
                if (received.Count > 0)
                {
                    byte[] tokenBytes = new byte[received.Count];
                    Array.ConstrainedCopy(buffer.Array, 0, tokenBytes, 0, received.Count);
                    return new WebSocket(socket, new RandomId(tokenBytes));
                }

            }
            return null;
        }

        private async Task<System.Net.WebSockets.ClientWebSocket> Connect(string url)
        {
            System.Net.WebSockets.ClientWebSocket webSocket = new System.Net.WebSockets.ClientWebSocket();
           
            await webSocket.ConnectAsync(new Uri(url), CancellationToken.None);
            return webSocket;
        }

        private HttpListener Setup()
        {
            var listener = new HttpListener();
            Uri _original = new Uri(Address);
            if (Port <= 0)
                Port = _original.Port;
            if (Port <= 0)
                Port = 80;
            string scheme = _original.Scheme;
            if (scheme == "ws")
                scheme = "http";
            else if (scheme == "wss")
                scheme = "https";
            string final = $"{scheme}://{_original.Host}:{Port}{_original.PathAndQuery}";
            listener.Prefixes.Add(final.EndsWith("/") ? final : final + "/");
            return listener;

        }

        private async void AcceptConnection(IAsyncResult ar)
        {
            System.Net.WebSockets.WebSocketContext context = null; 
            try
            {
                HttpListenerContext listenerContext = ((HttpListener)ar.AsyncState).EndGetContext(ar);
                if (listenerContext.Request.IsWebSocketRequest)
                {
                    try
                    {
                        context = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                    }
                    catch (Exception ex)
                    {
                        listenerContext.Response.StatusCode = 500;
                        listenerContext.Response.Close();
                        Diagnostic.DiagnosticCenter.Instance.Log?.LogException<Exception>(ex);
                    }
                }
                else
                {
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Diagnostic.DiagnosticCenter.Instance.Log?.LogException<Exception>(ex);
            }
            finally
            {
                httpListener?.BeginGetContext(new AsyncCallback(AcceptConnection), httpListener);
            }
            if(context != null)
                CollectSocket(context.WebSocket);
        }

        private async Task StartNatPortMappingAsync()
        {
            var disc = new NatDiscoverer();
            nat = await disc.DiscoverDeviceAsync();
            portMap = new Mapping(Protocol.Tcp, Port, Port, "Socket Server Map");
            await nat?.CreatePortMapAsync(portMap);
        }

        private void StartNatPortMapping()
        {
            var disc = new NatDiscoverer();
            nat = disc.DiscoverDeviceAsync().Result;
            portMap = new Mapping(Protocol.Tcp, Port, Port, "Socket Server Map");
            Task.WaitAll(nat?.CreatePortMapAsync(portMap));
        }

        private void ReleaseClients()
        {
            Parallel.ForEach(csockets.Values, (socket) => {
                try
                {
                    socket.Disconnect();
                }
                catch (Exception) { } //Ignoring any errors, we are shutting down anyways
            });
        }

        private void CollectSocket(System.Net.WebSockets.WebSocket socket)
        {
            ISocket isocket = new WebSocket(socket, RandomId.Generate());
            while (!csockets.TryAdd(isocket.Id, isocket))
                isocket = new WebSocket(socket, RandomId.Generate());
            isocket.OnSocketDisconnected += RemoveSocket;
            SendServerToken(isocket);
            OnConnectionRequested?.Invoke(isocket);
        }

        private void RemoveSocket(ISocket socket)
        {
            ISocket isocket;
            if (csockets.TryRemove(socket.Id, out isocket))
                isocket.OnSocketDisconnected -= RemoveSocket;
        }

        private static void SendServerToken(ISocket socket)
        {
            socket.Send(socket.Id.ToArray());
        }

        private static string GetClientAddress(string address, int port)
        {
            Uri _original = new Uri(address);
            if (port <= 0)
                port = _original.Port;
            if (port <= 0)
                port = 80;
            return $"{_original.Scheme}://{_original.Host}:{port}{_original.PathAndQuery}";
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                StopListening();
                disposedValue = true;
            }
        }

        #endregion

        #endregion

    }
}

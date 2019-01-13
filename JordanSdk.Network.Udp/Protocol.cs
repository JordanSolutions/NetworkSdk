using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using JordanSdk.Network.Core;
using System.Linq;
using Open.Nat;

namespace JordanSdk.Network.Udp
{
    /// <summary>
    /// This class is the UDP implementation of IProtocol, simplifies UDP network management for all possible operations such as listening, accepting incoming connections, connecting as a client to a remote server and much more.
    /// </summary>
    public class UdpProtocol : IProtocol
    {

        #region Private Fields

        private ConcurrentDictionary<RandomId, UdpSocket> csockets = new ConcurrentDictionary<RandomId, UdpSocket>();
        private Socket listener;
        private IPEndPoint _localEndpoint;
        private IPEndPoint _remoteEndpoint;
        private bool disposedValue = false; // To detect redundant calls
        private NatDevice nat;
        private Mapping portMap;

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
        /// True when listening for incoming connections.
        /// </summary>
        public bool Listening { get; private set; } = false;


        /// <summary>
        /// Use this property to specify the port bind to for listening, or connecting to a remote server. This field is required for either kind of socket to be created (server/client).
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Use this property for specifying the local Interface to bind to either for server or client connections. Defaults to IPV4, Any IP address (0.0.0.0).
        /// Examples: IPV4 Local Host - '127.0.0.1', IPV6 Local Host - '::1'
        /// </summary>
        public string Address { get; set; } = "127.0.0.1";

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public UdpProtocol() { }

        /// <summary>
        /// Default Destructor
        /// </summary>
        ~UdpProtocol()
        {
            Dispose(false);
        }
        
        #endregion

        #region Events

        /// <summary>
        /// This event is invoked when a client attempts to establish a socket connection in order to give an opportunity to the protocol owner to accept or reject the connection.
        /// </summary>
        public event SocketConnectedDelegate OnConnectionRequested;

        #endregion

        #region Public Functions

        /// <summary>
        /// Starts listening for incoming connection.
        /// </summary>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        public void Listen(bool enableNatTraversal = false)
        {
            if (Listening)
                return;
            _localEndpoint = new IPEndPoint(IPAddress.Parse(Address), Port);
            listener = Setup(_localEndpoint);
            if (enableNatTraversal)
                StartNatPortMapping();
            StartListening(listener);
        }

        /// <summary>
        /// Starts listening for incoming connection.
        /// </summary>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        public async Task ListenAsync(bool enableNatTraversal = false)
        {
            if (Listening)
                return;
            _localEndpoint = new IPEndPoint(IPAddress.Parse(Address), Port);
            listener = Setup(_localEndpoint);
            if (enableNatTraversal)
                await StartNatPortMappingAsync();
            StartListening(listener);
        }

        /// <summary>
        /// Stops listening for incoming connections.
        /// </summary>
        public void StopListening()
        {
            Listening = false;
            ReleaseClients();
            listener?.Shutdown(SocketShutdown.Both);
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
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        /// <returns>Returns an instance of TCP Socket</returns>
        public async Task<ISocket> ConnectAsync(string remoteIp, int remotePort, bool enableNatTraversal = false)
        {
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
            _localEndpoint = new IPEndPoint(IPAddress.Parse(Address ?? (remoteEndPoint.AddressFamily == AddressFamily.InterNetwork ? "127.0.0.1" : "::1")), Port);
            Socket socket = Setup(_localEndpoint);
            if (enableNatTraversal)
                await StartNatPortMappingAsync();
            return await SetupClientToken(socket, remoteEndPoint);
        }

        /// <summary>
        /// Initiates an asynchronous connection to a remote server, calling callback once the connection is established.
        /// <param name="callback">Callback invoked once the connection is established.</param>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        /// </summary>
        /// <returns>Returns an instance of TCP Socket</returns>
        public void ConnectAsync(Action<ISocket> callback, string remoteIp, int remotePort, bool enableNatTraversal = false)
        {
            Task.Run(async () =>
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
                _localEndpoint = new IPEndPoint(IPAddress.Parse(Address ?? (remoteEndPoint.AddressFamily == AddressFamily.InterNetwork ? "127.0.0.1" : "::1")), Port);

                Socket socket = Setup(_localEndpoint);
                var result = await SetupClientToken(socket, remoteEndPoint);
                if (enableNatTraversal)
                    await StartNatPortMappingAsync();
                if (result != null)
                    callback?.Invoke(result);

            });
        }

        /// <summary>
        /// Initiates a synchronous connection to a remote server.
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        /// </summary>
        /// <returns>Returns an instance of TCP Socket</returns>
        public ISocket Connect(string remoteIp, int remotePort, bool enableNatTraversal = false)
        {
            var remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
            _localEndpoint = new IPEndPoint(IPAddress.Parse(string.IsNullOrWhiteSpace(Address) ? (remoteEndPoint.AddressFamily == AddressFamily.InterNetwork ? "127.0.0.1" : "::1") : Address), Port);
            Socket socket = Setup(_localEndpoint);
            var result = SetupClientToken(socket, remoteEndPoint).Result;
            if (enableNatTraversal)
                StartNatPortMapping();
            return result;
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

        private Socket Setup(IPEndPoint endPoint)
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
                socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            SetupCommonFields(socket);
            socket.Bind(endPoint);
            return socket;
        }
        private void SetupCommonFields(Socket socket)
        {
            socket.Ttl = 255;
            socket.SendBufferSize = BUFFER_SIZE;
            socket.ReceiveBufferSize = BUFFER_SIZE;
            socket.SendTimeout = SEND_TIMEOUT;
            socket.ReceiveTimeout = RECEIVE_TIMEOUT;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
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

        private void StartListening(Socket listener)
        {
            if (Listening)
                return;
            Listening = true;
            byte[] connectData = new byte[BUFFER_SIZE];
            EndPoint endPoint = new IPEndPoint(_localEndpoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, Port);
            listener.BeginReceiveFrom(connectData, 0, BUFFER_SIZE, 0, ref endPoint, new AsyncCallback(AcceptConnection), new AsyncState()
            {
                Socket = listener,
            });
        }

        private void AcceptConnection(IAsyncResult ar)
        {
            UdpSocket client = null;
            bool succeed = false;
            AsyncState asyncState = ar.AsyncState as AsyncState;
            bool shoudContinue = asyncState.Socket.IsBound;
            if (!shoudContinue || !Listening)
                return;
            EndPoint senderIp = new IPEndPoint(IPAddress.Any, 0);
            try
            {

                int? count = asyncState.Socket?.EndReceiveFrom(ar, ref senderIp);
                var existing = csockets.Values.FirstOrDefault(p => p.RemoteEndPoint == senderIp);
                if (existing == null && count == 1)
                    succeed = CollectSocket(senderIp as IPEndPoint, out client);
                else
                    Diagnostic.DiagnosticCenter.Instance.Log?.LogException(new ArgumentException(succeed ? "Unable to setup the client connection request." : "UDP Connection request should be one byte long"));
            }catch(System.Net.Sockets.SocketException ex)
            {
                /*
                 * Ignoring 10040, this exception happens often when client and server are on the same machine using the same IP address and port and this loop 
                 * receives data larger than the initial expected connection buffer size.
                 * Also any incoming UDP package that is larger than the connection expected size will be ignored and discarded.
                 */
                if (ex.ErrorCode != 10040)
                    Diagnostic.DiagnosticCenter.Instance.Log?.LogException(ex);

            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Diagnostic.DiagnosticCenter.Instance.Log?.LogException(ex);
            }
            finally
            {
                byte[] dummy = new byte[1];
                EndPoint endPoint = new IPEndPoint(_localEndpoint.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, Port);
                listener.BeginReceiveFrom(dummy, 0, dummy.Length, 0, ref endPoint, new AsyncCallback(AcceptConnection), new AsyncState() { Socket = listener });
            }
            if (succeed)
                OnConnectionRequested?.Invoke(client);
            else
                Diagnostic.DiagnosticCenter.Instance.Log?.LogException(new Exception(succeed ? "Unable to setup the client connection request." : "UDP Connection request should be one byte long"));
        }


        private bool CollectSocket(IPEndPoint endPoint, out UdpSocket isocket)
        {
            Socket socket = Setup(_localEndpoint);
            isocket = new UdpSocket(socket, endPoint, RandomId.Generate());
            while (!csockets.TryAdd(isocket.Id, isocket))
                isocket = new UdpSocket(socket, endPoint, RandomId.Generate());
            isocket.OnSocketDisconnected += RemoveSocket;
            isocket.Connected = true;
            try
            {
                SendServerToken(isocket);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void RemoveSocket(ISocket socket)
        {
            UdpSocket isocket;
            if (csockets.TryRemove(socket.Id, out isocket))
                isocket.OnSocketDisconnected -= RemoveSocket;
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
            nat?.CreatePortMapAsync(portMap).RunSynchronously();
        }


        private static void SendServerToken(UdpSocket socket)
        {
            var throwAway = socket.SendAsync(socket.Id.ToArray());
        }

        private static async Task<UdpSocket> SetupClientToken(Socket socket, EndPoint endPoint)
        {
            byte[] buffer = new byte[BUFFER_SIZE];
            UdpSocket result = null;
            TaskCompletionSource<bool> sendTask = new TaskCompletionSource<bool>();
            socket.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, 0, ref endPoint, (ar) =>
            {
                int count = socket.EndReceiveFrom(ar, ref endPoint);
                if (count > 0)
                {
                    byte[] received = new byte[count];
                    Array.Copy(buffer, 0, received, 0, count);
                    result = new UdpSocket(socket, endPoint as IPEndPoint, new RandomId(received)) { Connected = true };
                    sendTask.SetResult(true);
                }
                else
                    sendTask.SetResult(false);
            }, null);
            socket.BeginSendTo(new byte[] { 1 }, 0, 1, 0, endPoint, (ar) =>
            {
                int sent = socket.EndSendTo(ar);
            }, null);

            await Task.WhenAny(sendTask.Task, Task.Delay(10000));
            return result;
        }


        #region IDisposable Support / Finalizer

        /// <summary>
        /// Releases all connected sockets and closes the listener if open.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && Listening)
                    StopListening();
                nat?.DeletePortMapAsync(portMap);
                portMap = null;
                nat = null;
                disposedValue = true;
            }
        }
        #endregion

        #endregion
    }
}

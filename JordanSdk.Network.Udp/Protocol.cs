using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using JordanSdk.Network.Core;

namespace JordanSdk.Network.Udp
{
    /// <summary>
    /// This class is the TCP Implementation of IProtocol, and simplifies TCP network management for all possible operations such as listening and accepting incoming connections, connecting as a client to a remote server and much more.
    /// </summary>
    public class UdpProtocol : IProtocol<UdpSocket>, IDisposable
    {
        #region Private Fields

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

        #region Private Properties
        private Socket Listener { get; set; }

        private ManualResetEventSlim ResetEvent { get; set; } = new ManualResetEventSlim(false);

        private IPEndPoint Endpoint { get; set; }

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
                ResetEvent.Reset(); //Releases the thread
                try
                {
                    listenerManager.Cancel(false);
                }
                catch (Exception) { }
                {
                    
                }
                Listener?.Close();
                Listener?.Dispose();
                Listener = null;
            }
        }


        /// <summary>
        /// Initiates an asynchronous connection to a remote server.
        /// </summary>
        /// <returns>Returns an instance of TCP Socket</returns>
        public async Task<UdpSocket> ConnectAsync()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(Address ?? "127.0.0.1"), Port);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            return await SetupClientToken(socket, endPoint);
        }

        /// <summary>
        /// Initiates an asynchronous connection to a remote server, calling callback once the connection is established.
        /// </summary>
        /// <returns>Returns an instance of TCP Socket</returns>
        public async void ConnectAsync(Action<UdpSocket> callback)
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(Address ?? "127.0.0.1"), Port);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            var result = await SetupClientToken(socket, endPoint);
            if (result != null)
                callback?.Invoke(result);
        }

        /// <summary>
        /// Initiates a synchronous connection to a remote server.
        /// </summary>
        /// <returns>Returns an instance of TCP Socket</returns>
        public UdpSocket Connect()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse(Address ?? "127.0.0.1"), Port);
            var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            var result = SetupClientToken(socket, endPoint).Result;
            return result;
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
                Endpoint = new IPEndPoint(IPAddress.Any, Port);
            else
                Endpoint = new IPEndPoint(IPAddress.Parse(Address), Port);

            Listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
            SetupCommonListenerFields();
        }

        private void SetupIPV6()
        {
            if(string.IsNullOrWhiteSpace(Address))
                Endpoint = new IPEndPoint(IPAddress.IPv6Any, Port);
            else
                Endpoint = new IPEndPoint(IPAddress.Parse(Address), Port);

            Listener = new Socket(AddressFamily.InterNetworkV6,
               SocketType.Dgram, ProtocolType.Udp);
            Listener.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            SetupCommonListenerFields();
        }

        private void SetupCommonListenerFields()
        {
            Listener.Ttl = 255;
            Listener.SendBufferSize = BUFFER_SIZE;
            Listener.ReceiveBufferSize = BUFFER_SIZE;
            Listener.SendTimeout = SEND_TIMEOUT;
            Listener.ReceiveTimeout = RECEIVE_TIMEOUT;
            Listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Listener.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
        }

        private void StartListening()
        {
            if (Listening)
                return;
            Listening = true;
            Listener.Bind(Endpoint);
            listenerManager = new CancellationTokenSource();
            var throwAway = Task.Run(() =>
            {
                try
                {
                    while (Listening)
                    {
                        byte[] connectData = new byte[1];
                        EndPoint endPoint = new IPEndPoint(Endpoint.Address, Endpoint.Port);
                        Listener.BeginReceiveFrom(connectData, 0, 1, 0, ref endPoint, new AsyncCallback(AcceptConnection), new AsyncTupleState<Socket, IPEndPoint>()
                        {
                            State = Listener,
                            Data = endPoint as IPEndPoint
                        });
                        ResetEvent.Wait();
                        ResetEvent.Reset();
                    }
                }
                catch (ThreadAbortException) { }
            }, listenerManager.Token);
        }

        private void AcceptConnection(IAsyncResult ar)
        {

            bool succeed = false;
            AsyncTupleState<Socket,IPEndPoint> asyncState = ar.AsyncState as AsyncTupleState<Socket, IPEndPoint>;
            EndPoint endPoint = asyncState.Data as EndPoint;
            try
            {
                
                int? count = asyncState.State?.EndReceiveFrom(ar,ref endPoint);
                succeed = count.HasValue && count == 1;
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) {
                Diagnostic.DiagnosticCenter.Instance.Log?.LogException<Exception>(ex);
            }
            finally
            {
                ResetEvent?.Set();
            }
            try
            {
                if (succeed)
                    CollectSocket(asyncState.State, endPoint as IPEndPoint);
            }
            catch (Exception ex)
            {
                Diagnostic.DiagnosticCenter.Instance.Log?.LogException<Exception>(ex);
            }
        }

       
        private void CollectSocket(Socket socket, IPEndPoint endPoint)
        {
            ISocket isocket = new UdpSocket(socket, endPoint, Guid.NewGuid().ToString("N"));
            while (!csockets.TryAdd(isocket.Token, isocket))
                isocket = new UdpSocket(socket, endPoint, Guid.NewGuid().ToString("N"));
            isocket.OnSocketDisconnected += RemoveSocket;
            SendServerToken(socket,isocket.Token,endPoint);
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

        ~UdpProtocol() {
            Dispose(false);
        }

        

        #endregion

        private static async Task<UdpSocket> SetupClientToken(Socket socket, EndPoint endPoint)
        {

            byte[] buffer = new byte[BUFFER_SIZE];
            UdpSocket result = null;
            SocketError error;
            TaskCompletionSource<bool> sendTask = new TaskCompletionSource<bool>();
            socket.BeginSendTo(new byte[] { 1 }, 0, 1, 0, endPoint, (sendState) =>
            {
                var asyncState = sendState.AsyncState as AsyncState<Socket>;
                int sent = asyncState.State.EndSendTo(sendState);
                socket.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, 0, ref endPoint, (receiveState) =>
                {
                    int received = socket.EndReceiveFrom(receiveState, ref endPoint);
                    if (received > 0)
                    {
                        byte[] tokenBytes = new byte[received];
                        Array.ConstrainedCopy(buffer, 0, tokenBytes, 0, received);
                        result = new UdpSocket(socket, endPoint as IPEndPoint, Encoding.ASCII.GetString(tokenBytes)) { Connected = true };
                        sendTask.SetResult(true);
                    }
                    else
                        sendTask.SetResult(false);

                }, socket);
                //if (sent == 1)
                //    sendTask.SetResult(true);
                //else
                //    sendTask.SetResult(false);
            }, new AsyncState<Socket>() { CallBack = sendTask, State = socket });
            await sendTask.Task;
            return result;
        }

        private static void SendServerToken(Socket socket, string token, EndPoint remote)
        {
            byte[] _token = Encoding.ASCII.GetBytes(token);
            socket.SendTo(_token, remote);
        }

        //private static void AsyncConnectCallback(IAsyncResult ar)
        //{
        //    AsyncState<Socket> state = ar.AsyncState as AsyncState<Socket>;
        //    try
        //    {
        //        state.State.EndConnect(ar);
        //        if (state.CallBack != null && state.CallBack is Action<UdpSocket>)
        //            (state.CallBack as Action<UdpSocket>).Invoke(SetupClientToken(state.State));
        //        else if (state.CallBack != null && state.CallBack is TaskCompletionSource<UdpSocket>)
        //            (state.CallBack as TaskCompletionSource<UdpSocket>).SetResult(SetupClientToken(state.State));
        //    }
        //    catch (Exception ex)
        //    {
        //        if (ar.AsyncState is TaskCompletionSource<UdpSocket>)
        //            (ar.AsyncState as TaskCompletionSource<UdpSocket>).SetException(ex);
        //        else
        //            throw ex;
        //    }
        //}

        #endregion
    }
}

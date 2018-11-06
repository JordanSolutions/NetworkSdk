//By Erik Jordan January 9, 2018, Simplified and modernized from my original 2006 source code.
using System;
using System.Threading.Tasks;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// The interface simplifies networking connection management for both server and client connections through an easy to use, yet powerful contract, enabling for 2 different asynchronous models for flexibility; standard awaitable Task as well as callback mechanisms that will fire once the operation completes, at the same time it exposes the same functions for synchronous operations.
    /// </summary>
    /// <typeparam name="T">ISocket implementation.</typeparam>
    public interface IProtocol<T> where T : ISocket
    {

        /// <summary>
        /// This property is used for when NAT port mapping / port forwarding is needed. We use Open.Nat which is a great library in order to achieve this. Your implementation needs not to worried about managing port mapping.
        /// </summary>
        bool EnableNatTraversal { get; set; }

        /// <summary>
        /// Local port the socket will be bound to. For connection oriented and connectionless protocol, this property is required when creating a server protocol kind. When creating a client socket, this property is ignored for connection oriented protocols, but for connectionless protocols it is advised that a value is set although not required.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Specifies the IP Address (v4 or v6) to use, defaults to 127.0.0.1 if none specified.
        /// </summary>
        string Address { get; set; }


        /// <summary>
        /// This flag is set to true when the protocol is listening for incoming connections.
        /// </summary>
        bool Listening { get; }

        /// <summary>
        /// Event that is fired when a client connects.
        /// </summary>
        event SocketConnectedDelegate OnConnectionRequested;

        /// <summary>
        /// Stops listening for incoming connections and closes all current client connections.
        /// </summary>
        void StopListening();

        /// <summary>
        /// Starts listening for incoming connections over 'Address' and 'Port'.
        /// </summary>
        void Listen();

        /// <summary>
        /// Starts listening for incoming connections over 'Address' and 'Port'.
        /// </summary>
        Task ListenAsync();

        /// <summary>
        /// Initiates a client connection.
        /// </summary>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <returns>Returns an instance of the created client ISocket if the connection was established, null otherwise.</returns>
        Task<T> ConnectAsync(string remoteIp, int remotePort);

        /// <summary>
        /// Initiates a client connection.
        /// </summary>
        /// <param name="callback">Callback invoked once the connection is established. T will be null if the connection did not succeed.</param>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <returns>Returns an instance of the created client ISocket</returns>
        void ConnectAsync(Action<T> callback, string remoteIp, int remotePort);

        /// <summary>
        /// Initiates a client connection.
        /// </summary>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <returns>Returns an instance of the created client ISocket if the connection was established, null otherwise.</returns>
        T Connect(string remoteIp, int remotePort);

    }
}

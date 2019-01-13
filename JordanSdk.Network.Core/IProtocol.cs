//By Erik Jordan January 9, 2018, Simplified and modernized from my original 2006 source code.
using System;
using System.Threading.Tasks;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// The interface simplifies networking connection management for both server and client connections through an easy to use, yet powerful contract, enabling for 2 different asynchronous models for flexibility; standard awaitable Task as well as callback mechanisms that will fire once the operation completes, at the same time it exposes the same functions for synchronous operations.
    /// </summary>
    public interface IProtocol : IDisposable
    {

        /// <summary>
        /// Local port used to listen for incoming connections.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Specifies the IP Address (v4 or v6) to use or URL in the case of websockets, defaults to the loopback (127.0.0.1 / localhost) address.
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
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        void Listen(bool enableNatTraversal = false);

        /// <summary>
        /// Starts listening for incoming connections over 'Address' and 'Port'.
        /// </summary>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        Task ListenAsync(bool enableNatTraversal = false);

        /// <summary>
        /// Initiates a client connection to a remote server.
        /// </summary>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        /// <returns>Returns an instance of the created client ISocket if the connection was established, null otherwise.</returns>
        Task<ISocket> ConnectAsync(string remoteIp, int remotePort, bool enableNatTraversal = false);

        /// <summary>
        /// Initiates a client connection to a remote server.
        /// </summary>
        /// <param name="callback">Callback invoked once the connection is established. T will be null if the connection did not succeed.</param>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        /// <returns>Returns an instance of the created client ISocket</returns>
        void ConnectAsync(Action<ISocket> callback, string remoteIp, int remotePort, bool enableNatTraversal = false);

        /// <summary>
        /// Initiates a client connection to a remote server.
        /// </summary>
        /// <param name="remoteIp">Remote server IP address to connect to.</param>
        /// <param name="remotePort">Remote server IP port to connect to.</param>
        /// <param name="enableNatTraversal">Set to true to try and enable NAT traversal via configuring your router for port forwarding.</param>
        /// <returns>Returns an instance of the created client ISocket if the connection was established, null otherwise.</returns>
        ISocket Connect(string remoteIp, int remotePort, bool enableNatTraversal = false);

    }
}

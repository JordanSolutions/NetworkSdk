//By Erik Jordan January 9, 2018, Simplified and modernized from original 2008 source code, also by Erik Jordan

using System;
using System.Threading.Tasks;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// The IProtocol interface that simplifies networking connection management for both server and client connections through an easy to use, yet powerful contract. 
    /// This interface allows for 2 different asynchronous models for flexibility, standard awaitable Task as well as callback mechanisms that will fire once the operation completes, at the same time it exposes the same functions for synchronous operations.
    /// </summary>
    /// <typeparam name="T">ISocket implementation.</typeparam>
    public interface IProtocol<T> where T : ISocket
    {
        /// <summary>
        /// When overwritten in a derived class, this property specifies the port to listen or connect to in the case of a client connection.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// When overwritten in a derived class, this property is used for connecting to a remote server, or restrict the IP address (TCP-UDP)/URL(WebSockets - Others) the server will bind to.
        /// </summary>
        string Address { get; set; }

        /// <summary>
        /// This flag is set to true when the protocol is listening for incoming connections.
        /// </summary>
        bool Listening { get; }

        /// <summary>
        /// Event that is fired when a client connects.
        /// </summary>
        event UserConnectedDelegate OnConnectionRequested;

        /// <summary>
        /// Stops listening for incoming connections and closes all current client connections.
        /// </summary>
        void StopListening();

        /// <summary>
        /// Starts listening for incoming connections.
        /// </summary>
        void Listen();

        /// <summary>
        /// Initiates a connection as a client.
        /// </summary>
        /// <returns>Returns an instance of T</returns>
        Task<T> ConnectAsync();

        /// <summary>
        /// Initiates a connection as a client.
        /// </summary>
        /// <returns>Returns an instance of T</returns>
        void ConnectAsync(Action<T> callback);

        /// <summary>
        /// Initiates a connection as a client.
        /// </summary>
        /// <returns>Returns an instance of T</returns>
        T Connect();

    }
}

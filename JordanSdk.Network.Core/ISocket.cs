//By Erik Jordan January 9, 2018, Simplified and modernized from my original 2006 source code.
using System;
using System.Threading.Tasks;
namespace JordanSdk.Network.Core
{
    /// <summary>
    /// This interface provides contracts that simplifies sending and receiving data through the network.
    /// </summary>
    public interface ISocket
    {
        /// <summary>
        /// Unique identifier assigned by the server.
        /// </summary>
        RandomId Id { get; }

        /// <summary>
        /// This property indicates the connected state of your socket.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Event invoked when the connection is lost or closed.
        /// </summary>
        event DisconnectedDelegate OnSocketDisconnected;

        /// <summary>
        /// Use this function to receive data from the network asynchronously. This function will invoke the provided action once data is received. 
        /// </summary>
        /// <param name="callback">Callback to be invoked when data is received.</param>
        void ReceiveAsync(Action<byte[]> callback);

        /// <summary>
        /// Use this Task oriented function to receive data from the network asynchronously.
        /// </summary>
        /// <returns>Returns an INetworkBuffer object with data received.</returns>
        Task<byte[]> ReceiveAsync();

        /// <summary>
        /// Use this function to receive data from the network synchronously. This function blocks until data is received or until underlying socket receive time out.
        /// </summary>
        /// <returns>Returns a Network Buffer with the data received.</returns>
        byte[] Receive();

        /// <summary>
        /// Use this Task oriented function to send data over the network asynchronously.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes written to the network.</returns>
        Task<int> SendAsync(byte[] data);

        /// <summary>
        /// Use this function to send data over the network asynchronously. This method will invoke the provided action once the operation completes.
        /// </summary>
        /// <param name="data">INetworkBuffer containing the data to be sent.</param>
        /// <param name="callback">Callback invoked once the write operation concludes, containing the amount of bytes sent through the network.</param>
        void SendAsync(byte[] data, Action<int> callback);

        /// <summary>
        /// Use this function to send a buffer over the network. This method blocks until all data in buffer is sent or until underlying socket receive time out.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes sent.</returns>
        int Send(byte[] data);

        /// <summary>
        /// Disconnects the socket asynchronously.
        /// </summary>
        /// <returns>Returns a Task that can be used to wait for the operation to complete.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Use this function to disconnect the socket asynchronously. Once the operation succeeds, the provided callback will be invoked.
        /// </summary>
        /// <param name="callback">Callback invoked when the socket is disconnected.</param>
        void DisconnectAsync(Action callback);

        /// <summary>
        /// Disconnects the socket blocking until the operation completes.
        /// </summary>
        void Disconnect();

    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
//By Erik Jordan January 9, 2018, Simplified and modernized from original 2008 source code, also by Erik Jordan

using System.Threading.Tasks;
namespace JordanSdk.Network.Core
{
    public interface ISocket
    {

        string Token { get; }

        /// <summary>
        /// Event invoked when the connection is lost or purposely closed.
        /// </summary>
        event DisconnectedDelegate OnSocketDisconnected;

        /// <summary>
        /// When overwritten in a derived class, initiates an asynchronous network read operation that will conclude by invoking callback.
        /// </summary>
        /// <param name="callback">Callback to be invoked when the received operation concludes.</param>
        void ReceiveAsync(Action<INetworkBuffer> callback);

        /// <summary>
        /// When overwritten in a derived class, initiates an awaitable asynchronous network read.
        /// </summary>
        /// <returns>Returns an INetworkBuffer object with data received.</returns>
        Task<INetworkBuffer> ReceiveAsync();

        /// <summary>
        /// When overwritten in a derived class, blocks until data is received.
        /// </summary>
        /// <returns>Returns a Network Buffer with the data received.</returns>
        INetworkBuffer Receive();

        /// <summary>
        /// When overwritten in a derived class, initiates an awaitable asynchronous network write operation.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes written to the network.</returns>
        Task<int> SendAsync(INetworkBuffer data);

        /// <summary>
        /// When overwritten in a derived class, writes data to the network and calls callback once the write operation concludes.
        /// </summary>
        /// <param name="data">INetworkBuffer containing the data to be sent.</param>
        /// <param name="callback">Callback invoked once the write operation concludes.</param>
        void SendAsync(INetworkBuffer data, Action<int> callback);

        /// <summary>
        /// When overwritten in a derived class, blocks until all data in buffer is sent.
        /// </summary>
        /// <param name="data">Data to be written to the network.</param>
        /// <returns>Returns the amount of bytes written.</returns>
        int Send(INetworkBuffer data);

        /// <summary>
        /// When overwritten in a derived class, this awaitable function disconnects the socket connection.
        /// </summary>
        /// <returns>An awaitable class.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// When overwritten in a derived class, this awaitable function disconnects the socket connection.
        /// </summary>
        /// <param name="callback">Callback invoked when the socket is disconnected.</param>
        /// <returns>An awaitable class.</returns>
        void DisconnectAsync(Action callback);

        /// <summary>
        /// When overwritten in a derived class, disconnects the socket blocking until the operation completes.
        /// </summary>
        void Disconnect();

    }
}

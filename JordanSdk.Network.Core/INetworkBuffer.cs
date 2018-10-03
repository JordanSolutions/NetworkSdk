using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// Buffer that contains data to be written, or read from the network stream.
    /// </summary>
    public interface INetworkBuffer
    {
        /// <summary>
        /// Size of the buffer in bytes.
        /// </summary>
        int Size { get; }
        /// <summary>
        /// When overwritten in a derived class, represents the amount of bytes already copied into buffer.
        /// </summary>
        int Received { get; }
        /// <summary>
        /// When overwritten in a derived class, indicates that all bytes have been copied into buffer.
        /// </summary>
        bool Completed { get; }
        
        /// <summary>
        /// When overwritten in a derived class, returns an array with received bytes.
        /// </summary>
        /// <returns></returns>
        byte[] GetBuffer();

        /// <summary>
        /// Reads from buffer the number of bytes specified by length, starting from buffer current position. When length exceeds the amount of bytes remaining to be read, the returned array will contain only the remaining bytes.
        /// </summary>
        /// <param name="length">Maximum number of bytes to read from buffer.</param>
        /// <returns>Returns an array of bytes from buffer. When no more bytes are left to read, this function will return null.</returns>
        byte[] Read(int length);

        /// <summary>
        /// Resets buffer current position to zero. 
        /// </summary>
        void ResetPosition();

        /// <summary>
        /// When overwritten in a derived class copies the byte array to the buffer, from the last byte already written. Note that buffer is an accumulated stream and the position in the stream is equal to received, for this reason, data can not be larger than size or size minus received.
        /// </summary>
        /// <param name="data">byte array to copy into buffer.</param>
        void Append(byte[] data);
        /// <summary>
        /// When overwritten in a derived class, Copies a byte array starting from source position (element index in data), and containing the amount of elements specified in source size. When source size is not specified (null) the reminder of position and size of the array will be used as size.
        /// </summary>
        /// <param name="data">Array to copy into buffer.</param>
        /// <param name="position">Element position in data to start copying from.</param>
        /// <param name="size">Optional size of the elements to copy from data.</param>
        void AppendConstrained(byte[] data, uint position, uint? size);

        /// <summary>
        /// When overwritten in a derived class, resizes the buffer. If new size is less than original size, the buffer will be truncated and an array copy operation will take place degrading performance. When new size is larger than original, the buffer is extended without requiring bytes to be transfered to a new array.
        /// </summary>
        /// <param name="newSize">New buffer size.</param>
        void Resize(int newSize);

        /// <summary>
        /// When implemented in a derived class, provides access to creating a duplicate of the network buffer.
        /// </summary>
        /// <returns>Returns a network buffer containing a copy of the original.</returns>
        INetworkBuffer Clone();

        /// <summary>
        /// Use this function to get the checksum of the data stored in buffer. Note that this will only include written data, is advice to call this function only when all data has been written.
        /// </summary>
        /// <returns>A byte array with 16 elements containing the checksum of the stored data.</returns>
        byte[] GetChecksum();
    }
}

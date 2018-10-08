using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace JordanSdk.Network.Core
{

    /// <summary>
    /// The network buffer class is a simple implementation of the INetwork Buffer contract, and provides Read and Write access to an array of bytes used for sending or receiving network packages.
    /// While you can initiate a network buffer with just the intended size, and write bytes to the buffer, is not until all bytes are written that the buffer becomes readable, with the exception of
    /// 'ToArray' function that will return all bytes written regardless. Reading from the buffer is forward only, to start reading from the beginning of the buffer again, you will need to call 'ResetPosition' function.
    /// </summary>
    public class NetworkBuffer : INetworkBuffer, IDisposable
    {

        #region Private Fields
        private MemoryStream buffer;
        private int size;
        private int received;
        private object locker = new object();
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes an empty buffer with the specified size.
        /// </summary>
        /// <param name="size">Size of the buffer.</param>
        public NetworkBuffer(int size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException("size", "Size can not be less than one.");
            this.size = size;
            received = 0;
            buffer = size > 0 ? new MemoryStream(size) : new MemoryStream();
        }

        /// <summary>
        /// Initializes a buffer with the specified size, and copies the array provided into the buffer.
        /// </summary>
        /// <param name="size">Size of the buffer.</param>
        /// <param name="initialData">Array not larger than size.</param>
        public NetworkBuffer(int size, byte[] initialData)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException("size", "Size can not be less than one.");
            this.size = size;
            if (size == initialData.Length)
                buffer = new MemoryStream(initialData);
            else
            {
                buffer = new MemoryStream(size);
                buffer.Write(initialData, 0, initialData.Length);
            }
            received = initialData.Length;
        }


        /// <summary>
        /// Initializes an empty buffer using the provided memory stream.
        /// </summary>
        /// <param name="data"></param>
        public NetworkBuffer(MemoryStream data)
        {
            data.Position = 0;
            this.size = (int)data.Length;
            received = (int)data.Length;
            buffer = data;
        }

        /// <summary>
        /// Initializes the buffer copying all data from the provided instance.
        /// </summary>
        /// <param name="instance"></param>
        public NetworkBuffer(INetworkBuffer instance)
        {
            this.size = instance.Size;
            this.received = instance.Received;
            buffer = new MemoryStream(instance.ToArray());
        }

        #endregion

        /// <summary>
        /// Size of the buffer in bytes.
        /// </summary>
        public int Size { get { return size; } }

        /// <summary>
        ///Represents the amount of bytes already copied into buffer.
        /// </summary>
        public int Received { get { return received; } }

        /// <summary>
        /// Indicates that all bytes have been copied into buffer.
        /// </summary>
        public bool Completed => size > 0 && received == size;

        /// <summary>
        /// Copies the byte array to the buffer, from the last byte already written. Note that buffer is an accumulated stream and the position in the stream is equal to received, for this reason, data can not be larger than size or size minus received.
        /// </summary>
        /// <param name="data">byte array to copy into buffer.</param>
        public void Append(byte[] data)
        {
            if (disposedValue)
                throw new ObjectDisposedException("This buffer has been disposed.");
            lock (locker)
            {
                if (received + data.Length > size)
                    throw new ArgumentOutOfRangeException("data", "Buffer overflow, the amount of data to be copied is larger than buffer size.");
                buffer.Write(data, 0, data.Length);
                received += data.Length;
                if (Completed)
                    buffer.Position = 0;
            }
        }

        /// <summary>
        /// Copies a byte array starting from source position (element index in data), and containing the amount of elements specified in source size. When source size is not specified (null) the reminder of position and size of the array will be used as size.
        /// </summary>
        /// <param name="data">Array to copy into buffer.</param>
        /// <param name="position">Element position in data to start copying from.</param>
        /// <param name="size">Optional size of the elements to copy from data.</param>
        /// <exception cref="ObjectDisposedException">This exception is thrown when the network buffer has been disposed.</exception>
        /// <exception cref="ArgumentNullException">This exception is thrown when data is null or its length is zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">This exception is thrown when the amount of bytes to be copied into buffer exceeds the remaining bytes available to be written in buffer.</exception>
        public void AppendConstrained(byte[] data, uint position = 0, uint? size = null)
        {
            if (disposedValue)
                throw new ObjectDisposedException("This buffer has been disposed.");

            lock (locker)
            {
                if (data == null || data.Length == 0)
                    throw new ArgumentNullException("data", "Data can not be null or zero length.");
                if (!size.HasValue)
                    size = (uint)data.Length - position;
                if (received + size > this.size)
                    throw new ArgumentOutOfRangeException("data", "Buffer overflow, the amount of data to be copied is larger than buffer size.");
                buffer.Write(data, (int)position, (int)size.Value);
                received += (int)size.Value;
            }
        }

        /// <summary>
        /// Returns an array containing all received bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This exception is thrown when the network buffer has been disposed.</exception>
        /// <returns>Array of bytes.</returns>
        public byte[] ToArray()
        {
            if (disposedValue)
                throw new ObjectDisposedException("This buffer has been disposed.");

            lock (locker)
            {
                if (received == 0)
                    return new byte[0];
                return buffer.ToArray();
            }
        }

        /// <summary>
        /// Reads from buffer the number of bytes specified by length, starting from buffer current position. Returned array size will be constrained to the remaining bytes to be read, when length exceeds the size of remaining bytes.
        /// </summary>
        /// <param name="length">Maximum number of bytes to read from buffer.</param>
        /// <exception cref="ObjectDisposedException">This exception is thrown when the network buffer has been disposed.</exception>
        /// <returns>Returns an array with data from buffer and size not larger than length. Array size will be constrained to the amount of bytes remaining to be read, or length.</returns>
        public byte[] Read(int length)
        {
            if (disposedValue)
                throw new ObjectDisposedException("This buffer has been disposed.");

            lock (locker)
            {
                if (!Completed)
                    throw new Exception("Not all bytes has been written into buffer, until this occurs reading or modifying the position of the buffer is not allowed. If your intentions are not to continue writing to buffer, resize the buffer to the same size or less than Received Property( Buffer.Resize(Buffer.Received)).");
                if (buffer.Position >= received)
                    return null;
                var _result = new byte[received < length ? received : buffer.Position + length < received ? length : received - (int)buffer.Position];
                buffer.Read(_result, 0, _result.Length);
                return _result;
            }
        }

        /// <summary>
        /// Resets buffer current position to zero. 
        /// </summary>
        /// <exception cref="ObjectDisposedException">This exception is thrown when the network buffer has been disposed.</exception>
        /// <exception cref="InvalidOperationException">This exception is thrown when the buffer is incomplete, the position of the buffer can not be changed until all bytes have been written.</exception>
        public void ResetPosition()
        {
            if (disposedValue)
                throw new ObjectDisposedException("This buffer has been disposed.");

            lock (locker)
            {
                if (!Completed)
                    throw new InvalidOperationException("Not all bytes has been written into buffer, until this occurs reading or modifying the position of the buffer is not allowed. If your intentions are not to continue writing to buffer, resize the buffer to the same size or less than Received Property( Buffer.Resize(Buffer.Received)).");
                buffer.Position = 0;
            }
        }

        /// <summary>
        /// Resizes the buffer. 
        /// If new size is less than original size, the buffer will be truncated and an array copy operation will take place degrading performance. 
        /// When new size is larger than original, the buffer is extended without requiring bytes to be transfered to a new array.
        /// </summary>
        /// <param name="newSize">New buffer size.</param>
        /// <exception cref="ObjectDisposedException">This exception is thrown when the network buffer has been disposed.</exception>
        public void Resize(int newSize)
        {
            if (disposedValue)
                throw new ObjectDisposedException("This buffer has been disposed.");
            
            lock (locker)
            {
                if (newSize == size)
                    return;
               
                var _copySize = newSize > received ? received : newSize;
                byte[] newArray = new byte[_copySize];
                buffer.Read(newArray, 0, _copySize);
                buffer.Dispose();
                buffer = new MemoryStream(newSize);
                buffer.Write(newArray, 0, newArray.Length);
                received = _copySize;
                size = newSize;
            }
        }


        /// <summary>
        /// Use this function to get the checksum of the data stored in buffer. Note that this will only include written data, is advice to call this function only when all data has been written.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This exception is thrown when the network buffer has been disposed.</exception>
        /// <returns>A byte array with 16 elements containing the checksum of the stored data.</returns>
        public byte[] GetChecksum()
        {
            if (disposedValue)
                throw new ObjectDisposedException("This buffer has been disposed.");
            using (MD5 hashCreator = MD5.Create())
                return hashCreator.ComputeHash(buffer.ToArray());
        }

        /// <summary>
        /// Use this function to creating a duplicate of the network buffer.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This exception is thrown when the network buffer has been disposed.</exception>
        /// <returns>Returns a network buffer containing a copy of the original.</returns>
        public INetworkBuffer Clone()
        {
            if (disposedValue)
                throw new ObjectDisposedException("This buffer has been disposed.");

            return new NetworkBuffer(this);
        }


        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    buffer.Dispose();
                }
                disposedValue = true;
            }
        }

        /// <summary>
        /// Release all resources allocated.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}

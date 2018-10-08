using System;
using System.Collections.Generic;
using System.Text;

namespace JordanSdk.Network.Core
{
    /// <summary>
    /// This class is used to generate a small random unique identifier, used to identify packages or connected clients.
    /// </summary>
    public class RandomId
    {
        #region Fields
        private const int ID_SIZE = 5;
        private byte[] _id;
        private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        #endregion

        #region Public Properties
        public int Length => _id.Length;
        #endregion

        #region Constructor

        public RandomId(byte[] id) { _id = id; }

        public RandomId(byte[] data, int index) {
            _id = new byte[ID_SIZE];
            Array.Copy(data, index, _id,0, ID_SIZE);
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Generates a new random Package Id.
        /// </summary>
        /// <returns>Returns a new random package id.</returns>
        public static RandomId Generate()
        {
            Random rnd = new Random();
            byte[] id = new byte[ID_SIZE];
            for (int i = 0; i < ID_SIZE; i++)
                id[i] = (byte)chars[rnd.Next(chars.Length)];
            return new RandomId(id);
        }
        #endregion

        /// <summary>
        /// Returns the generated id as a string.
        /// </summary>
        /// <returns>String representation of the generated id.</returns>
        public override string ToString()
        {
            return Encoding.ASCII.GetString(_id);
        }

        /// <summary>
        /// Returns the ID byte array.
        /// </summary>
        /// <returns>Returns the ID byte array.</returns>
        public byte[] ToArray() { return _id; }

        /// <summary>
        /// Compares a package id with any object of type PackageId, String or Byte Array
        /// </summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>True if ids are equal, false otherwise.</returns>
        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            return ToString() == (obj is string ? obj as string : obj is RandomId ? (obj as RandomId).ToString() : obj is byte[] ? Encoding.ASCII.GetString(obj as byte[]) : null);
        }

    }
}

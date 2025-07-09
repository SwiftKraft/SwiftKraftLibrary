// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    /// <summary>
    /// Contains arbitrary user-defined data in the form of a byte array.
    /// </summary>
    public struct CustomPayload
    {
        /// <summary>
        /// Maximum length of the custom payload in bytes.
        /// </summary>
        public const int MaxCustomPayloadLen = 512;

        /// <summary>
        /// Empty payload.
        /// </summary>
        public static CustomPayload Empty = new();

        /// <summary>
        /// Returns true if the payload contains no data.
        /// </summary>
        public readonly bool IsEmpty => bytes == null || bytes.Length == 0;

        /// <summary>
        /// Returns the payload's underlying byte array.
        /// </summary>
        public readonly byte[] Bytes => bytes;

        /// <summary>
        /// Decodes the payload's byte array to a string using UTF8 encoding.
        /// </summary>
        /// <remarks>Returns empty string if the payload is empty.</remarks>
        public readonly string AsString => IsEmpty ? string.Empty : System.Text.Encoding.UTF8.GetString(bytes);

        private byte[] bytes;

        /// <summary>
        /// Creates a new CustomPayload from a byte array.
        /// </summary>
        /// <param name="bytes">The payload data.</param>
        public CustomPayload(byte[] bytes)
        {
            this.bytes = bytes;
        }

        /// <summary>
        /// Creates a new CustomPayload from a string using UTF8 encoding.
        /// </summary>
        /// <param name="value">The string that will be encoded and stored as payload data.</param>
        public CustomPayload(string value)
        {
            bytes = System.Text.Encoding.UTF8.GetBytes(value);
        }
    }
}

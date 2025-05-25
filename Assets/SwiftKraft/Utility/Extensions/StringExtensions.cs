using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace SwiftKraft.Utils
{
    public static class StringExtensions
    {
        /// <summary>
        /// Compresses the string.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public static byte[] Compress(this string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return gZipBuffer;
        }

        /// <summary>
        /// Decompresses the byte array.
        /// </summary>
        /// <param name="compressedText">The compressed byte array.</param>
        /// <returns></returns>
        public static string Decompress(this byte[] compressedText)
        {
            byte[] gZipBuffer = compressedText;
            using var memoryStream = new MemoryStream();
            int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
            memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

            var buffer = new byte[dataLength];

            memoryStream.Position = 0;
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                gZipStream.Read(buffer, 0, buffer.Length);
            }

            return Encoding.UTF8.GetString(buffer);
        }

        public static readonly Regex RichTextRegex = new(@"<.*?>", RegexOptions.Compiled);

        public static int Length(this string input, bool ignoreRichText = true)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            if (ignoreRichText)
            {
                // Strip rich text tags like <b>, </color>, <size=20>, etc.
                input = RichTextRegex.Replace(input, "");
            }

            return input.Length;
        }
    }
}

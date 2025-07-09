// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Utils
{
    /// <summary>
    /// Hash calculations.
    /// </summary>
    internal static class HashCalc
    {
        /// <summary>
        /// Calculate the SHA1 hash of a string. Used for the compound schema.
        /// </summary>
        /// <param name="text">String to hash</param>
        /// <returns>Hexadecimal string of the hashed value</returns>
        public static string SHA1Hash(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var sha = System.Security.Cryptography.SHA1.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.Default.GetBytes(text));
            var sb = new System.Text.StringBuilder(hash.Length * 2);

            foreach (var b in hash)
            {
                _ = sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}

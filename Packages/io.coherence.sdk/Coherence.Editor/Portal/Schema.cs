// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Editor.Portal
{
    [System.Serializable]
    public class Schema
    {
#pragma warning disable 649
        public string schema; // base64 contents
        public string hash; // sha256
#pragma warning restore 649

        internal string Contents { get; private set; }

#if UNITY
        public static Schema GetFromSchemaAsset(SchemaAsset asset)
        {
            return GetFromString(asset.raw);
        }
#endif

        public static Schema GetFromString(string raw)
        {
            var contents = raw.Replace("\r\n", "\n");
            string base64 = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(contents));

            return new Schema
            {
                schema = base64,
                hash = CalcSHA256Hash(base64),
                Contents = contents,
            };
        }

        // used for individual schema hashes

        private static string CalcSHA256Hash(string text)
        {
            var sha = System.Security.Cryptography.SHA256.Create();
            var hash = sha.ComputeHash(System.Text.Encoding.Default.GetBytes(text));
            var sb = new System.Text.StringBuilder(hash.Length * 2);

            foreach (byte b in hash)
            {
                _ = sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}

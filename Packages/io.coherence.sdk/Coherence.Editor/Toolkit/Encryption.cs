using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;

namespace Coherence.Editor.Toolkit
{
    internal static class Encryption
    {
        private static string PrefEncryptionKey = "coherence.encryption.key";
        private static string PrefEncryptionIV = "coherence.encryption.IV";

        private static void SetAesKeys(AesManaged aes)
        {
            if (EditorPrefs.HasKey(PrefEncryptionKey))
            {
                aes.Key = Convert.FromBase64String(EditorPrefs.GetString(PrefEncryptionKey));
                aes.IV = Convert.FromBase64String(EditorPrefs.GetString(PrefEncryptionIV));
            }
            else
            {
                EditorPrefs.SetString(PrefEncryptionKey, Convert.ToBase64String(aes.Key));
                EditorPrefs.SetString(PrefEncryptionIV, Convert.ToBase64String(aes.IV));
            }
        }

        public static byte[] Encrypt(string plainText)
        {
            byte[] encrypted;
            // Create a new AesManaged.
            using (AesManaged aes = new AesManaged())
            {
                SetAesKeys(aes);

                // Create encryptor
                ICryptoTransform encryptor = aes.CreateEncryptor();
                // Create MemoryStream
                using (MemoryStream ms = new MemoryStream())
                {
                    // Create crypto stream using the CryptoStream class. This class is the key to encryption
                    // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream
                    // to encrypt
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Create StreamWriter and write data to a stream
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }

                        encrypted = ms.ToArray();
                    }
                }
            }
            // Return encrypted data
            return encrypted;
        }

        public static string Decrypt(byte[] cipherText)
        {
            string plaintext = null;
            // Create AesManaged
            using (AesManaged aes = new AesManaged())
            {
                SetAesKeys(aes);

                // Create a decryptor
                ICryptoTransform decryptor = aes.CreateDecryptor();
                // Create the streams used for decryption.
                using (MemoryStream ms = new MemoryStream(cipherText))
                {
                    // Create crypto stream
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        // Read crypto stream
                        using (StreamReader reader = new StreamReader(cs))
                        {
                            plaintext = reader.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }
    }
}

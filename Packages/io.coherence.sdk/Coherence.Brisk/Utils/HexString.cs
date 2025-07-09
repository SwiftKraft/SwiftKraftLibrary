// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Utils
{
    using System;

    public static class HexConverter
    {
        public static string ToString(byte[] data, int maxLength)
        {
            byte[] dataToShow = data;
            string suffix = "";

            if (data.Length > maxLength)
            {
                byte[] copyData = new byte[maxLength];
                Array.Copy(data, copyData, maxLength);
                dataToShow = copyData;
                suffix = "...";
            }

            string hexString = BitConverter.ToString(dataToShow).Replace("-", string.Empty);

            return $"{hexString}{suffix}";
        }
    }
}

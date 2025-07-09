// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using System;

namespace Coherence.Common
{
    internal static class VersionUtils
    {
        public static string ConsumeVersionComponentFromString(string value, ref int cursor, Func<char, bool> isEnd)
        {
            int length = 0;
            for (int i = cursor; i < value.Length; i++)
            {
                if (isEnd(value[i]))
                    break;

                length++;
            }

            int newIndex = cursor;
            cursor += length;
            return value.Substring(newIndex, length);
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.NativeLauncher
{
    using System.Collections.Generic;
    using System.Text;

    internal class LineSplitter
    {
        private StringBuilder stringBuilder;
        private List<string> linesCache;
        private int currentLinePos;
        private bool lastCarriageReturn;

        public LineSplitter(int size)
        {
            stringBuilder = new StringBuilder(size);
            linesCache = new List<string>(32);
        }

        public IReadOnlyList<string> Append(char[] data, int length)
        {
            stringBuilder.Append(data, 0, length);

            linesCache.Clear();

            var currentIndex = currentLinePos;
            var lineStart = 0;
            var len = stringBuilder.Length;

            // Skip a beginning '\n' character of new block if last block ended with '\r'
            if (lastCarriageReturn && len > 0 && stringBuilder[0] == '\n')
            {
                currentIndex = 1;
                lineStart = 1;
                lastCarriageReturn = false;
            }

            while (currentIndex < len)
            {
                // Check line breaks: \n - UNIX, \r\n - DOS,  \r - Mac
                var ch = stringBuilder[currentIndex];
                if (ch == '\r' || ch == '\n')
                {
                    var line = stringBuilder.ToString(lineStart, currentIndex - lineStart);
                    lineStart = currentIndex + 1;

                    // Skip the "\n" character following "\r" character
                    if (ch == '\r' && lineStart < len && stringBuilder[lineStart] == '\n')
                    {
                        lineStart++;
                        currentIndex++;
                    }

                    linesCache.Add(line);
                }

                currentIndex++;
            }

            if (len > 0 && stringBuilder[len - 1] == '\r')
            {
                lastCarriageReturn = true;
            }

            // Keep the rest characters which can't form a new line in string builder.
            if (lineStart < len)
            {
                if (lineStart == 0)
                {
                    // No break lines, in this case we cache the position for the next time.
                    currentLinePos = currentIndex;
                }
                else
                {
                    stringBuilder.Remove(0, lineStart);
                    currentLinePos = 0;
                }
            }
            else
            {
                stringBuilder.Length = 0;
                currentLinePos = 0;
            }

            return linesCache;
        }

        public string Flush()
        {
            var line = stringBuilder.ToString();
            stringBuilder.Length = 0;
            currentLinePos = 0;
            lastCarriageReturn = false;

            return line;
        }
    }
}

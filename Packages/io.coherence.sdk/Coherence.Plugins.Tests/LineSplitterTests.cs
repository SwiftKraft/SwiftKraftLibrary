// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Coherence.Tests;
    using NativeLauncher;
    using NUnit.Framework;

    public class LineSplitterTests : CoherenceTest
    {
        private static readonly List<string> TestLines = new()
        {
            "[INF] line splitter info line 1: abc",
            "[INF] line splitter info line 2: 1234 f342",
            "[INF] line splitter info line 3: ok",
            "[WRN] line splitter warning: incorrect format",
            "[INF] line splitter info line 4: ok",
            "[INF] line splitter info line 5: def",
            "[ERR] line splitter error line: unexpected condition",
        };

        [TestCase("\n", 4)]
        [TestCase("\r\n", 3)]
        [TestCase("\r", 5)]
        [Description("Append splits lines correctly as the are processed")]
        public void Append_SplitsLinesCorrectly(string lineDelimiter, int splitCount)
        {
            var lineSplitter = new LineSplitter(1024);

            // Append delimiter at the end to complete the line.
            var mergedText = string.Join(lineDelimiter, TestLines) + lineDelimiter;
            var splitSize = mergedText.Length / splitCount;

            var splits = new List<string>();
            for (var i = 0; i < splitCount; ++i)
            {
                var length = i != splitCount - 1 ? splitSize : mergedText.Length - (i * splitSize);
                splits.Add(mergedText.Substring(i * splitSize, length));
            }

            List<string> outputLines = new();
            foreach (var split in splits)
            {
                var lines = lineSplitter.Append(split.ToCharArray(), split.Length);
                outputLines.AddRange(lines);
            }

            Assert.That(outputLines, Is.EquivalentTo(TestLines));
        }

        [TestCase("\n", 6)]
        [TestCase("\r\n", 2)]
        [TestCase("\r", 1)]
        [Description("Flush returns remaining buffered text if the last line is incomplete")]
        public void Flush_ReturnsRemainingBufferedText(string lineDelimiter, int splitCount)
        {
            var lineSplitter = new LineSplitter(1024);

            // Do not append delimiter at the end to keep the last line incomplete.
            var mergedText = string.Join(lineDelimiter, TestLines);
            var splitSize = mergedText.Length / splitCount;

            var splits = new List<string>();
            for (var i = 0; i < splitCount; ++i)
            {
                var length = i != splitCount - 1 ? splitSize : mergedText.Length - (i * splitSize);
                splits.Add(mergedText.Substring(i * splitSize, length));
            }

            foreach (var split in splits)
            {
                _ = lineSplitter.Append(split.ToCharArray(), split.Length);
            }

            var remainingText = lineSplitter.Flush();
            Assert.That(remainingText, Is.EqualTo(TestLines.Last()));
        }
    }
}

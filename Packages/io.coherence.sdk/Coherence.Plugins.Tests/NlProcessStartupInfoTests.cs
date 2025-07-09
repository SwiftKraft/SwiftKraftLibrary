// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.Tests
{
    using System.Collections.Generic;
    using Coherence.Tests;
    using NativeLauncher;
    using NUnit.Framework;

    public class NlProcessStartupInfoTests : CoherenceTest
    {
        public struct TestInput
        {
            public string Arguments;
            public List<string> Expected;
        }

        private static IEnumerable<TestInput> TestCases => new TestInput[]
        {
            new ()
            {
                Arguments = "",
                Expected = new List<string>{}
            },
            new ()
            {
                Arguments = null,
                Expected = new List<string>{}
            },
            new ()
            {
                Arguments = "--env dev --port 1234 --log-file \"log.txt\"",
                Expected = new List<string>{"--env", "dev", "--port", "1234", "--log-file", "log.txt"}
            },
            new ()
            {
                Arguments = "--arg1 \"some value\" --arg2 \"c:\\path\\to\\file.txt\"",
                Expected = new List<string>{"--arg1", "some value", "--arg2", "c:\\path\\to\\file.txt"}
            },
        };

        [Test]
        [TestCaseSource(nameof(TestCases))]
        public void ArgumentsParsing(TestInput input)
        {
            var startupInfo = new NlProcessStartupInfo("the/path/exec", input.Arguments);

            Assert.That(startupInfo.Arguments, Is.EquivalentTo(input.Expected));
        }

    }
}

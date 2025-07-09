// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using Cloud;
    using Coherence.Tests;

    [TestFixture]
    public class GameServersCommandLineArgumentsTests : CoherenceTest
    {
        [Test]
        public void ParsingArguments_Should_Succeed_When_GivenCorrectValues()
        {
            var args = new[]
            {
                "--coherence-game-port", "3000",
                "--coherence-api-port", "4000",
                "--coherence-play-api-endpoint", "https://api.prod.coherence.io/v1/play",
                "--coherence-region", "eu",
                "--coherence-auth-token", "very.secret.token",
                "--coherence-state-file", "/var/state",
                "--coherence-id", "1",
                "--coherence-tag", "test",
                "--coherence-kv", "eyJmb28iOiAiYmFyIn0K", // {"foo": "bar"} encoded as Base64
                "--non-coherence-arg", "123",
                "maybe-not-an-argument", "something else",
            };
            var cla = new CommandLineArguments(args);
            Assert.AreEqual(3000, cla.GamePort);
            Assert.AreEqual(4000, cla.ApiPort);
            Assert.AreEqual("https://api.prod.coherence.io/v1/play", cla.PlayApiEndpoint);
            Assert.AreEqual("eu", cla.Region);
            Assert.AreEqual("very.secret.token", cla.AuthToken);
            Assert.AreEqual("/var/state", cla.StateFile);
            Assert.AreEqual("1", cla.Id);
            Assert.AreEqual("test", cla.Tag);
            Assert.AreEqual(new Dictionary<string, string> { { "foo", "bar" }, }, cla.KV);
        }

        [Test]
        public void ParsingArguments_Should_FailGracefully_When_GivenBadInputs()
        {
            var args = new[]
            {
                "--coherence-game-port", "not-a-number",
                "--coherence-api-port", "not-a-number",
                "--coherence-kv", "invalid-base64",
            };
            var cla = new CommandLineArguments(args);
            Assert.AreEqual(0, cla.GamePort);
            Assert.AreEqual(0, cla.ApiPort);
            Assert.AreEqual(null, cla.PlayApiEndpoint);
            Assert.AreEqual(null, cla.Region);
            Assert.AreEqual(null, cla.AuthToken);
            Assert.AreEqual(null, cla.StateFile);
            Assert.AreEqual(null, cla.Id);
            Assert.AreEqual(null, cla.Tag);
            Assert.AreEqual(null, cla.KV);
        }

        [Test]
        public void NonStandardArguments_Can_BeAccessed()
        {
            var args = new[]
            {
                "--coherence-game-port", "3000",
                "--mode", "server",
            };
            var cla = new CommandLineArguments(args);
            Assert.AreEqual(3000, cla.GamePort);
            Assert.AreEqual("server", cla.Args.GetValueOrDefault("--mode", null));
        }
    }
}

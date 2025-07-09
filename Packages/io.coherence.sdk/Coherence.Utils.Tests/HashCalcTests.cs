// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Utils.Tests
{
    using NUnit.Framework;

    public class HashCalcTests
    {
        [Test]
        public void HashCalc_KnownHash_Matches()
        {
            var helloWorld = "5395ebfd174b0a5617e6f409dfbb3e064e3fdf0a";
            var hashed = HashCalc.SHA1Hash(nameof(helloWorld));
            Assert.That(helloWorld, Is.EqualTo(hashed));
        }

        [Test]
        public void HashCalc_NullString_DoesNotThrow_Returns_EmptyString()
        {
            string s = null;
            var hashed = HashCalc.SHA1Hash(s);
            Assert.That(hashed, Is.Empty);
        }
    }
}

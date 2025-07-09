// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Tests
{
    using NUnit.Framework;

    public class HostAuthorityTests
    {
        [TestCase(0, "")]
        [TestCase(HostAuthority.CreateEntities, "create-entities")]
        [TestCase(HostAuthority.ValidateConnection, "validate-connection")]
        [TestCase(HostAuthority.CreateEntities | HostAuthority.ValidateConnection, "create-entities,validate-connection")]
        public void GetCommaSeparatedFeaturesWorks(HostAuthority option, string expectedResult)
        {
            var result = option.GetCommaSeparatedFeatures();

            Assert.That(result, Is.EqualTo(expectedResult));
        }
    }
}

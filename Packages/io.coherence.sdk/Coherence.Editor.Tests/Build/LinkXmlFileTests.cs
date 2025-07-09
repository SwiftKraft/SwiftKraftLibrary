// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests
{
    using System.IO;
    using Coherence.Tests;
    using Editor;
    using NUnit.Framework;

    public sealed class LinkXmlFileTests : CoherenceTest
    {
        [Test, Description("Ensures that 'Packages/coherence/link.xml' exists.")]
        public void LinkXmlFile_Exists_Under_Packages() => Assert.That(File.Exists(Paths.linkXmlPackagesPath), Is.True);

        [Test, Description("Ensures that 'Assets/coherence/link.xml' does not exist. It should get removed after creating a build completes.")]
        public void LinkXmlFile_Does_Not_Exist_Under_Assets() => Assert.That(File.Exists(Paths.linkXmlAssetsPath), Is.False);

        [Test, Description("Ensures CopyLinkXmlFileUnderAssetsFolder returns.")]
        public void ManagedCodeStrippingUtils_CopyLinkXmlFileUnderAssetsFolder_Works()
        {
            ManagedCodeStrippingUtils.DeleteLinkXmlFileUnderAssetsFolder();
            var result = ManagedCodeStrippingUtils.CopyLinkXmlFileUnderAssetsFolder();
            Assert.That(result, Is.True);
            Assert.That(File.Exists(Paths.linkXmlAssetsPath), Is.True);
        }

        [Test]
        public void ManagedCodeStrippingUtils_DeleteLinkXmlFileUnderAssetsFolder_Works()
        {
            ManagedCodeStrippingUtils.CopyLinkXmlFileUnderAssetsFolder();
            var result = ManagedCodeStrippingUtils.DeleteLinkXmlFileUnderAssetsFolder();
            Assert.That(result, Is.True);
            Assert.That(File.Exists(Paths.linkXmlAssetsPath), Is.False);
        }

        public override void OneTimeTearDown()
        {
            ManagedCodeStrippingUtils.DeleteLinkXmlFileUnderAssetsFolder();
            base.OneTimeTearDown();
        }
    }
}

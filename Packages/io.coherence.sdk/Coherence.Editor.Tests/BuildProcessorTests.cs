// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests
{
    using System.IO;
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEngine;

    public class BuildProcessorTests : CoherenceTest
    {
        [Test]
        public void DeleteStreamingAssetsIfEmpty_Should_CleanUpFilesCreatedBy_EnsureStreamingAssetsPath()
        {
            var path = Application.streamingAssetsPath;
            var metaFilePath = PathUtils.GetMetaFilePath(path);

            BuildPreprocessor.EnsureStreamingAssetsPath();
            var directoryExisted = Directory.Exists(path);
            var metaFileExisted = File.Exists(metaFilePath);
            BuildPostprocessor.DeleteStreamingAssetsIfEmpty();
            var directoryExists = Directory.Exists(path);
            var metaFileExists = File.Exists(metaFilePath);

            Assert.IsTrue(directoryExisted);
            Assert.IsTrue(metaFileExisted);
            Assert.IsFalse(directoryExists);
            Assert.IsFalse(metaFileExists);
        }
    }
}

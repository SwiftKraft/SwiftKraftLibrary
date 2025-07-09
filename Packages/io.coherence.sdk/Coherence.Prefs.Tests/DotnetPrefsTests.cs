// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Prefs.Tests
{
    using Coherence.Prefs;
    using NUnit.Framework;
    using System.IO;
    using Coherence.Tests;

    public class DotnetPrefsTests : CoherenceTest
    {
        private string prefsFile;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            prefsFile = Path.GetTempFileName();
        }

        [TearDown]
        public override void TearDown()
        {
            try
            {
                File.Delete(prefsFile);
            }
            catch
            {
                // That's fine, we don't care on teardown
            }
            finally
            {
                base.TearDown();
            }
        }

        [Test]
        [TestCase(0f)]
        [TestCase(0.394801238497120498f)]
        [TestCase(-1f)]
        [TestCase(float.MaxValue)]
        [TestCase(float.MinValue)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void SetFloat_Works(float storedValue)
        {
            // Arrange
            string key = "float";
            DotnetPrefs sourcePrefs = new DotnetPrefs(prefsFile);

            // Act
            sourcePrefs.SetFloat(key, storedValue);
            sourcePrefs.Save();

            DotnetPrefs loadedPrefs = new DotnetPrefs(prefsFile);

            // Assert
            Assert.That(loadedPrefs.GetFloat(key), Is.EqualTo(storedValue));
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void SetInt_Works(int storedValue)
        {
            // Arrange
            string key = "int";
            DotnetPrefs sourcePrefs = new DotnetPrefs(prefsFile);

            // Act
            sourcePrefs.SetInt(key, storedValue);
            sourcePrefs.Save();

            DotnetPrefs loadedPrefs = new DotnetPrefs(prefsFile);

            // Assert
            Assert.That(loadedPrefs.GetInt(key), Is.EqualTo(storedValue));
        }

        [Test]
        [TestCase("hello")]
        [TestCase("")]
        [TestCase("          ")]
        public void SetString_Works(string storedValue)
        {
            // Arrange
            string key = "string";
            DotnetPrefs sourcePrefs = new DotnetPrefs(prefsFile);

            // Act
            sourcePrefs.SetString(key, storedValue);
            sourcePrefs.Save();

            DotnetPrefs loadedPrefs = new DotnetPrefs(prefsFile);

            // Assert
            Assert.That(loadedPrefs.HasKey(key));
            Assert.That(loadedPrefs.GetString(key), Is.EqualTo(storedValue));
        }

        [Test]
        public void KeyOverwriting_Works()
        {
            // Arrange
            string key = "key";
            float floatValue = 2f;
            DotnetPrefs sourcePrefs = new DotnetPrefs(prefsFile);

            // Act
            sourcePrefs.SetString(key, "hello");
            sourcePrefs.SetFloat(key, floatValue);
            sourcePrefs.Save();

            DotnetPrefs loadedPrefs = new DotnetPrefs(prefsFile);

            // Assert
            Assert.That(loadedPrefs.GetString(key, "default"), Is.EqualTo("default"));
            Assert.That(loadedPrefs.GetFloat(key), Is.EqualTo(floatValue));
        }
    }
}

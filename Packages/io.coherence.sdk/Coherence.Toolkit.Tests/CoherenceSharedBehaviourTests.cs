// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Toolkit.Tests
{
    using Coherence.Tests;
    using Internal;
    using NUnit.Framework;
    using UnityEngine;

    /// <summary>
    /// Unit tests for <see cref="CoherenceSharedBehaviour{TSharedBehaviour}"/>.
    /// </summary>
    public sealed class CoherenceSharedBehaviourTests : CoherenceTest
    {
        [Test]
        public void SharedInstance_IsNotNull()
        {
            var sharedInstance = Fake.SharedInstance;
            Assert.That(sharedInstance != null, Is.True);
        }

        [Test]
        public void SharedInstance_Returns_Same_Instance_Each_Time()
        {
            var firstTime = Fake.SharedInstance;
            var secondTime = Fake.SharedInstance;

            Assert.That(firstTime, Is.SameAs(secondTime));
        }

        [Test]
        public void GameObject_Has_HideFlags_HideAndDontSave()
        {
            var sharedInstance = Fake.SharedInstance;
            var gameObject = sharedInstance.gameObject;
            var hideFlags = gameObject.hideFlags;

            Assert.That(hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            Fake.DisposeSharedInstance(true);
        }

        private sealed class Fake : CoherenceSharedBehaviour<Fake>
        {
            public static new Fake SharedInstance => CoherenceSharedBehaviour<Fake>.SharedInstance;
        }
    }
}

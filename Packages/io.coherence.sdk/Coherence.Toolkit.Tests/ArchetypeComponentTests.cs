namespace Coherence.Toolkit.Tests
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public class ArchetypeComponentTests
    {
        private CoherenceSync sync;

        [SetUp]
        public void SetUp()
        {
            sync = new GameObject().AddComponent<CoherenceSync>();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(sync.gameObject);
        }

        /// <summary>
        /// <see cref="Coherence.Toolkit.Archetypes.ArchetypeComponent"/> needs an explicit overloaded default constructor,
        /// otherwise field initializers are skipped. This test validates that field initializers are executing.
        /// </summary>
        [Test]
        public void OnDeserialization_BindingsNotNull()
        {
            Assert.IsNotNull(sync);
            Assert.IsNotNull(sync.Archetype);
            Assert.IsNotNull(sync.Archetype.BoundComponents);
            foreach (var boundComponent in sync.Archetype.BoundComponents)
            {
                Assert.IsNotNull(boundComponent.Bindings);
            }

            using var serializedObject = new SerializedObject(sync);
            using var serializedProperty = serializedObject.FindProperty("archetype.boundComponents");
            serializedProperty.InsertArrayElementAtIndex(0);
            _ = serializedObject.ApplyModifiedProperties();

            Assert.IsNotNull(sync);
            Assert.IsNotNull(sync.Archetype);
            Assert.IsNotNull(sync.Archetype.BoundComponents);
            foreach (var boundComponent in sync.Archetype.BoundComponents)
            {
                Assert.IsNotNull(boundComponent.Bindings);
            }
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using System;
    using Archetypes;
    using NUnit.Framework;
    using Coherence.Tests;
    using UnityEngine;

    public class ToolkitArchetypeTests : CoherenceTest
    {
        [Test]
        public void Should_HaveEmptyList_When_InstantiatingArchetype()
        {
            var archetype = new BindingArchetypeData(SchemaType.Int, typeof(int), false);

            Assert.IsTrue(archetype.Fields != null && archetype.Fields.Count == 0);
        }

        [Test]
        public void Should_HaveOneLod_When_InstantiatingArchetypeAndAddingOneLod()
        {
            var archetype = new BindingArchetypeData(SchemaType.Int, typeof(int), false);

            archetype.AddLODStep(1);

            Assert.IsTrue(archetype.Fields != null && archetype.Fields.Count == 1);
        }

        [Test]
        public void UpdateBindableComponents_Does_Not_Use_IEquatable()
        {
            var gameObject = new GameObject();
            var coherenceSync = gameObject.AddComponent<CoherenceSync>();
            var archetype = new ToolkitArchetype();
            archetype.Setup(coherenceSync);
            gameObject.AddComponent<ComponentWithEqualsAlwaysTrue>();
            gameObject.AddComponent<ComponentWithEqualsAlwaysTrue>();

            archetype.UpdateBindableComponents();

            Assert.That(archetype.BoundComponents, Has.Count.EqualTo(3));
        }

        [Test]
        public void UpdateBindableComponents_Auto_Removes_Duplicate_Bound_Components_If_Identical_Data()
        {
            var gameObject = new GameObject();
            var coherenceSync = gameObject.AddComponent<CoherenceSync>();
            var archetype = new ToolkitArchetype();
            archetype.Setup(coherenceSync);
            var component = gameObject.AddComponent<ComponentWithEqualsAlwaysTrue>();
            archetype.BoundComponents.Add(new(component, 1));
            archetype.BoundComponents.Add(new(component, 1));

            archetype.UpdateBindableComponents();

            Assert.That(archetype.BoundComponents, Has.Count.EqualTo(2));
        }

        [Test]
        public void UpdateBindableComponents_Does_Not_Auto_Remove_Duplicate_Bound_Components_If_Different_Data()
        {
            var gameObject = new GameObject();
            var coherenceSync = gameObject.AddComponent<CoherenceSync>();
            var archetype = new ToolkitArchetype();
            archetype.Setup(coherenceSync);
            var component = gameObject.AddComponent<ComponentWithEqualsAlwaysTrue>();
            archetype.BoundComponents.Add(new(component, 1));
            archetype.BoundComponents.Add(new(component, 2));

            archetype.UpdateBindableComponents();

            Assert.That(archetype.BoundComponents, Has.Count.EqualTo(3));
        }

        private sealed class ComponentWithEqualsAlwaysTrue : MonoBehaviour, IEquatable<Component>
        {
            public bool Equals(Component other) => true;
            public override bool Equals(object obj) => true;
            public override int GetHashCode() => 0;
        }
    }
}


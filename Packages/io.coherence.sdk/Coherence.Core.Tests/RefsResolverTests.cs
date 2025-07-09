// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using Entities;
    using Log;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using Coherence.Tests;

    public class RefsResolverTests : CoherenceTest
    {
        private readonly Entity[] None = Array.Empty<Entity>();

        private RefsResolver refsResolver;
        private List<RefsInfo> refsInfo;
        private HashSet<Entity> knownEntities;
        private EntityRegistry entityRegistry;

        private Entity entityA = new Entity(2, 0, false);
        private Entity entityB = new Entity(4, 0, false);
        private Entity entityC = new Entity(8, 0, false);
        private Entity entityD = new Entity(16, 0, false);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            refsResolver = new RefsResolver(logger);

            refsInfo = new List<RefsInfo>();
            knownEntities = new HashSet<Entity>();
            entityRegistry = new EntityRegistry(knownEntities);
        }

        // LEGEND:
        // #A - known entity
        // @A - new entity
        // !A - missing entity

        [Test]
        [Description("Verifies that request is resolvable if the referenced entity is known.")]
        // @A --> #B
        public void ResolvableForKnown()
        {
            // Arrange
            WithKnownEntities(entityB);
            With(entityA).Referencing(entityB);

            // Act
            Resolve();

            // Assert
            AssertResolvable(entityA);
        }

        [Test]
        [Description("Verifies that request is resolvable if the referenced entity is not known even though referer is known.")]
        // #A --> !B
        public void UnResolvableForUnKnown()
        {
            // Arrange
            WithKnownEntities(entityA);
            With(entityA).Referencing(entityB);

            // Act
            Resolve();

            // Assert
            AssertResolvable(None);
        }

        [Test]
        [Description("Verifies that entity is resolvable which is referencing directly unresolvable entity because it's known")]
        // @A --> #B --> !C
        public void ResolvableForKnownDirectlyUnresolvableEntity()
        {
            // Arrange
            WithKnownEntities(entityB);
            With(entityA).Referencing(entityB);
            With(entityB).Referencing(entityC);

            // Act
            Resolve();

            // Assert
            AssertResolvable(entityA);
        }

        [Test]
        [Description("Verifies that entity is resolvable which is referencing undirectly unresolvable entity because it's known")]
        // @A --> #B --> @C --> !D
        public void ResolvableForKnownUnDirectlyUnresolvableEntity()
        {
            // Arrange
            WithKnownEntities(entityB);
            With(entityA).Referencing(entityB);
            With(entityB).Referencing(entityC);
            With(entityC).Referencing(entityD);

            // Act
            Resolve();

            // Assert
            AssertResolvable(entityA);
        }

        [Test]
        [Description("Verifies that request is resolvable if the referenced entity is an invalid entity (null).")]
        // @A --> (Invalid)
        public void ResolvableForInvalidEntity()
        {
            // Arrange
            With(entityA).Referencing(Entity.InvalidRelative);

            // Act
            Resolve();

            // Assert
            AssertResolvable(entityA);
        }

        [Test]
        [Description("Verifies that cyclic reference is resolvable even if entities additionally reference invalid entities.")]
        // @A --> (Invalid)
        // @B --> (Invalid)
        // @A <--> @B
        public void CyclicReferenceResolvableWithInvalidEntity()
        {
            // Arrange
            With(entityA).Referencing(entityB, Entity.InvalidRelative);
            With(entityB).Referencing(entityA, Entity.InvalidRelative);

            // Act
            Resolve();

            // Assert
            AssertResolvable(entityA, entityB);
        }

        [Test]
        [Description("Verifies that request is unresolvable if there's a reference chain with " +
                     "last link referring to an entity that is not known.")]
        // #A --> @B --> @C --> !D
        public void UnresolvableChain()
        {
            // Arrange
            WithKnownEntities(entityA);

            With(entityA).Referencing(entityB);
            With(entityB).Referencing(entityC);
            With(entityC).Referencing(entityD);

            // Act
            Resolve();

            // Assert
            AssertResolvable(None);
        }

        [Test]
        [Description("Verifies that request is resolvable if there's a reference chain with " +
                     "last link referring to an entity that is known.")]
        // @A --> @B --> #C
        public void ResolvableChain()
        {
            // Arrange
            WithKnownEntities(entityC);

            With(entityA).Referencing(entityB);
            With(entityB).Referencing(entityC);

            // Act
            Resolve();

            // Assert
            AssertResolvable(entityA, entityB);
        }

        [Test]
        [Description("Verifies that request is resolvable if there's a cyclic reference for " +
                     "created entities")]
        // @A --> @B
        // @B --> @A
        public void ResolvableSimpleCyclicReferencePending()
        {
            // Arrange
            With(entityA).Referencing(entityB);
            With(entityB).Referencing(entityA);

            // Act
            Resolve();

            // Assert
            AssertResolvable(entityA, entityB);
        }

        [Test]
        [Description("Verifies that request is unresolvable if an entity in the middle of the chain" +
                     "references missing entity.")]
        // @A --> @B --> #C
        //         \
        //          `-> !D
        public void UnresolvableChainWithSingleReferenceMissing()
        {
            // Arrange
            WithKnownEntities(entityC);

            With(entityA).Referencing(entityB);
            With(entityB).Referencing(entityC, entityD);

            // Act
            Resolve();

            // Assert
            AssertResolvable(None);
        }

        [Test]
        [Description("Verifies that request is resolvable if a known entity in the middle of the chain" +
                     "references missing entity.")]
        // @A --> #B --> #C
        //         \
        //          `-> !D
        public void ResolvableChainWithSingleReferenceMissing()
        {
            // Arrange
            WithKnownEntities(entityB, entityC);

            With(entityA).Referencing(entityB);
            With(entityB).Referencing(entityC, entityD);

            // Act
            Resolve();

            // Assert
            AssertResolvable(entityA);
        }

        [Test]
        [Description("Verifies that request is resolvable if there's a cyclic reference inside of a " +
                     "longer reference chain.")]
        // @A --> @B --> @C --> @D
        //         ^____________/
        public void ResolvableChainWithCyclicReference()
        {
            // Arrange
            With(entityA).Referencing(entityB);
            With(entityB).Referencing(entityC);
            With(entityC).Referencing(entityD);
            With(entityD).Referencing(entityB);

            // Act
            Resolve();

            // Assert
            AssertResolvable(entityA, entityB, entityC, entityD);
        }

        private void Resolve()
        {
            refsResolver.Resolve(refsInfo, entityRegistry);
        }

        private void WithKnownEntities(params Entity[] entities)
        {
            foreach (Entity entity in entities)
            {
                knownEntities.Add(entity);
            }
        }

        private void AssertResolvable(params Entity[] entities)
        {
            CollectionAssert.AreEquivalent(entities, refsResolver.ResolvableEntities);
        }

        private RefsInfoBuilder With(in Entity referer)
        {
            return new RefsInfoBuilder { Referer = referer, RefsInfo = refsInfo };
        }

        private struct RefsInfoBuilder
        {
            public Entity Referer;
            public List<RefsInfo> RefsInfo;

            public void Referencing(params Entity[] entities)
            {
                RefsInfo.Add(new RefsInfo(Referer, new List<Entity>(entities)));
            }
        }
    }
}

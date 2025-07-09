// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    using Coherence.Entities;
    using Coherence.Log;
    using Coherence.Tests;

    public class EntityIDGeneratorTests : CoherenceTest
    {
        [TestCase((ushort)1, (ushort)100)]
        [TestCase((ushort)5, (ushort)100)]
        [TestCase((ushort)10, (ushort)100)]
        [TestCase((ushort)99, (ushort)100)]
        [TestCase((ushort)(Entity.MaxIndices), (ushort)(Entity.MaxID))]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void CanGenerateIDs(ushort numIDs, ushort maxID)
        {
            var logger = Log.GetLogger<EntityIDGeneratorTests>();
            var entityIDGenerator = new EntityIDGenerator(1, maxID, Entity.Relative, logger);
            var entities = new Dictionary<ushort, Entity>();

            for (ushort i = 0; i < numIDs; i++)
            {
                var err = entityIDGenerator.GetEntity(out var entity);
                Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                Assert.False(entities.ContainsKey(entity.Index)); //Make sure they're unique
                entities.Add(entity.Index, entity);
            }
        }

        [Test]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void CanRecycleAllIDs()
        {
            var logger = Log.GetLogger<EntityIDGeneratorTests>();
            var entityIDGenerator = new EntityIDGenerator(1, Entity.MaxID, Entity.Relative, logger);
            var numIDs = Entity.MaxID;

            var entities = new Queue<Entity>();
            var indices = new HashSet<ushort>();

            var err = EntityIDGenerator.Error.None;

            for (ushort i = 0; i < numIDs; i++)
            {
                err = entityIDGenerator.GetEntity(out var entity);
                Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                entities.Enqueue(entity);
                Assert.False(indices.Contains(entity.Index));
                Assert.That(entity.Index, Is.EqualTo((ushort)(i + 1)));
                indices.Add(entity.Index);
            }

            // Can't get any more.
            err = entityIDGenerator.GetEntity(out var _);
            Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.OutOfIDs));

            for (ushort i = 0; i < numIDs; i++)
            {
                var entity = entities.Dequeue();
                indices.Remove(entity.Index);
                entityIDGenerator.ReleaseEntity(entity);
            }

            for (ushort i = 0; i < numIDs; i++)
            {
                err = entityIDGenerator.GetEntity(out var entity);
                Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                entities.Enqueue(entity);
                Assert.False(indices.Contains(entity.Index));
                Assert.That(entity.Index, Is.EqualTo((ushort)(i + 1)));
                indices.Add(entity.Index);
            }

            // Can't get any more.
            err = entityIDGenerator.GetEntity(out var _);
            Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.OutOfIDs));
        }

        [TestCase((ushort)1, 10)]
        [TestCase((ushort)10, 10)]
        [TestCase((ushort)100, 10)]
        [TestCase((ushort)Entity.MaxIndices, 1)]
        [TestCase((ushort)Entity.MaxIndices, 3)]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void CanRecycleIDsLinearly(ushort numIDs, int iterations)
        {
            var logger = Log.GetLogger<EntityIDGeneratorTests>();
            var endID = numIDs;
            var entityIDGenerator = new EntityIDGenerator(1, endID, Entity.Relative, logger);
            var lastIndex = numIDs == 1 ? 1 : Entity.MaxID;

            for (int i = 0; i < iterations; i++)
            {
                for (int j = 0; j < numIDs; j++)
                {
                    var err = entityIDGenerator.GetEntity(out var entity);
                    Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                    if (numIDs > 1)
                    {
                        Assert.That(entity.Index, Is.Not.EqualTo(lastIndex));
                    }
                    else
                    {
                        Assert.That(entity.Index, Is.EqualTo(lastIndex));
                    }
                    entityIDGenerator.ReleaseEntity(entity);
                    lastIndex = entity.Index;
                }
            }
        }

        [TestCase((ushort)1, 10)]
        [TestCase((ushort)10, 10)]
        [TestCase((ushort)100, 10)]
        [TestCase((ushort)Entity.MaxIndices, 10)]
        [TestCase((ushort)Entity.MaxIndices, 100)]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void CanRecycleIDsRandomly(ushort numIDs, int multiplier)
        {
            var random = new System.Random(1234);
            var logger = Log.GetLogger<EntityIDGeneratorTests>();
            var endID = numIDs;
            var entityIDGenerator = new EntityIDGenerator(1, endID, Entity.Relative, logger);
            var iterations = (int)Entity.MaxIndex * multiplier;

            var entities = new Queue<Entity>();
            var indices = new HashSet<ushort>();

            for (int i = 0; i < iterations; i++)
            {
                var rand = random.Next() % 2;
                if (entities.Count < numIDs
                    && (rand == 0 || entities.Count == 0))
                {
                    var err = entityIDGenerator.GetEntity(out var entity);
                    Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                    Assert.False(indices.Contains(entity.Index));
                    indices.Add(entity.Index);
                    entities.Enqueue(entity);
                }
                else if (entities.Count > 0)
                {
                    var entity = entities.Dequeue();
                    indices.Remove(entity.Index);
                    entityIDGenerator.ReleaseEntity(entity);
                }
            }
        }

        [TestCase((ushort)1, 2)]
        [TestCase((ushort)10, 11)]
        [TestCase((ushort)100, 101)]
        [TestCase((ushort)(Entity.MaxIndices), Entity.MaxIndices + 1)]
        public void TooManyIDs(ushort numIDs, int numToGenerate)
        {
            var logger = Log.GetLogger<EntityIDGeneratorTests>();
            var endID = numIDs;
            var entityIDGenerator = new EntityIDGenerator(1, endID, Entity.Relative, logger);
            var entities = new HashSet<ushort>();

            for (int i = 0; i < numToGenerate; i++)
            {
                var err = entityIDGenerator.GetEntity(out var entity);
                if (i < numToGenerate - 1)
                {
                    Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                }
                else
                {
                    Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.OutOfIDs));
                }
                Assert.False(entities.Contains(entity.Index)); //Make sure they're unique
                entities.Add(entity.Index);
            }
        }

        [TestCase((ushort)1)]
        [TestCase((ushort)10)]
        [TestCase((ushort)100)]
        [TestCase((ushort)(Entity.MaxIndices))]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void VersionIncrementsCorrectly(ushort numIDs)
        {
            var logger = Log.GetLogger<EntityIDGeneratorTests>();
            var endID = numIDs;
            var entityIDGenerator = new EntityIDGenerator(1, endID, Entity.Relative, logger);
            var entities = new Queue<Entity>();

            for (int v = 0; v < Entity.MaxVersions + 1; v++)
            {
                for (int i = 0; i < numIDs; i++)
                {
                    var err = entityIDGenerator.GetEntity(out var entity);
                    Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                    Assert.That(entity.Version, Is.EqualTo(v % Entity.MaxVersions));
                    entities.Enqueue(entity);
                }

                for (int i = 0; i < numIDs; i++)
                {
                    var entity = entities.Dequeue();
                    entityIDGenerator.ReleaseEntity(entity);
                }
            }
        }

        private void TestEdgeValues(EntityIDGenerator entityIDGenerator, ushort endID)
        {
            Assert.Throws<Exception>(() =>
            {
                entityIDGenerator.ReleaseEntity(new Entity(0, 0, Entity.Relative));
            });

            Assert.Throws<Exception>(() =>
            {
                entityIDGenerator.ReleaseEntity(new Entity((ushort)(endID + 1), 0, Entity.Relative));
            });
        }

        [TestCase((ushort)1)]
        [TestCase((ushort)10)]
        [TestCase((ushort)100)]
        [TestCase((ushort)(Entity.MaxIndices))]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void InvalidRecycle(ushort numIDs)
        {
            var logger = Log.GetLogger<EntityIDGeneratorTests>();
            var endID = numIDs;
            var entityIDGenerator = new EntityIDGenerator(1, endID, Entity.Relative, logger);

            TestEdgeValues(entityIDGenerator, endID);

            var entities = new Queue<Entity>();

            for (int i = 0; i < numIDs; i++)
            {
                var err = entityIDGenerator.GetEntity(out var entity);
                Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                entities.Enqueue(entity);
            }

            TestEdgeValues(entityIDGenerator, endID);

            for (int i = 0; i < numIDs; i++)
            {
                var entity = entities.Dequeue();
                entityIDGenerator.ReleaseEntity(entity);
            }

            for (ushort i = 0; i < Entity.MaxID; i++)
            {
                Assert.Throws<Exception>(() =>
                {
                    entityIDGenerator.ReleaseEntity(new Entity(i, 0, Entity.Relative));
                });
            }

            Assert.Throws<Exception>(() =>
            {
                var err = entityIDGenerator.GetEntity(out var entity);
                Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                entityIDGenerator.ReleaseEntity(new Entity(entity.Index, entity.Version, Entity.Absolute));
            });
        }

        [Test]
        public void IDGenerationAfterReset()
        {
            var logger = Log.GetLogger<EntityIDGeneratorTests>();
            var entityIDGenerator = new EntityIDGenerator(1, Entity.MaxID, Entity.Relative, logger);
            var entities = new List<Entity>();
            var numIDs = 10;

            for (ushort i = 0; i < numIDs; i++)
            {
                var err = entityIDGenerator.GetEntity(out var entity);
                Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                entities.Add(entity);
            }

            entityIDGenerator.Reset();

            for (ushort i = 0; i < numIDs; i++)
            {
                var err = entityIDGenerator.GetEntity(out var entity);
                Assert.That(err, Is.EqualTo(EntityIDGenerator.Error.None));
                Assert.That(entity, Is.EqualTo(entities[i]));
            }
        }

    }
}

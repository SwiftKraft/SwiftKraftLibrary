// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Tests;
    using Moq;
    using NUnit.Framework;

    /// <summary>
    /// Unit tests for <see cref="StorageOperationQueue.GetNextMutationsAndDeletions"/>.
    /// </summary>
    public sealed class StorageOperationQueueTests : CoherenceTest
    {
        private static StorageObjectId StorageId => (new("Test", "Storage"));
        private static StorageObjectId StorageId1 => (new("Test", "Storage1"));
        private static StorageObjectId StorageId2 => (new("Test", "Storage2"));
        private static StorageObjectId StorageId3 => (new("Test", "Storage3"));
        private static Key Key1 => "Key1";
        private static Key Key2 => "Key2";
        private static Key Key3 => "Key3";
        private static Key[] Keys1To3 => new[] { Key1, Key2, Key3 };
        private static Value Value1 => "Value1";
        private static Value Value2 => "Value2";
        private static Value Value3 => "Value3";
        private static StorageItem Item1 => new(Key1, Value1);
        private static StorageItem Item2 => new(Key2, Value2);
        private static StorageItem Item3 => new(Key3, Value3);
        private static StorageItem[] Items1To3 => new[] { Item1, Item2, Item3 };
        private static readonly TaskCompletionSource<bool> BoolTaskCompletionSource = new();
        private static List<TaskCompletionSource<bool>> BoolTaskCompletionSourceList => new();
        private static LoadTaskCompletionHandler TaskCompletionHandler => _ => { };
        private static List<LoadTaskCompletionHandler> TaskCompletionHandlerList => new();

        [Test]
        public async Task Is_Thread_Safe()
        {
            var mockStorageOperationBatchProcessor = new Mock<IStorageOperationBatchProcessor>(MockBehavior.Strict);
            mockStorageOperationBatchProcessor.Setup(x => x.LoadBatchAsync(It.IsAny<IEnumerable<StorageObjectQuery>>(), It.IsAny<CancellationToken>()))
                .Returns(new StorageOperation<StorageObject[]>(Task.FromResult(Array.Empty<StorageObject>())));
            mockStorageOperationBatchProcessor.Setup(x => x.SaveBatchAsync(It.IsAny<IEnumerable<StorageObjectMutation>>(), It.IsAny<CancellationToken>()))
                .Returns(new StorageOperation(Task.CompletedTask));
            mockStorageOperationBatchProcessor.Setup(x => x.DeleteBatchAsync(It.IsAny<IEnumerable<StorageObjectDeletion>>(), It.IsAny<CancellationToken>()))
                .Returns(new StorageOperation(Task.CompletedTask));
            var queue = new StorageOperationQueue(mockStorageOperationBatchProcessor.Object);

            var result = Parallel.For(0, 100, i =>
            {
                queue.EnqueueLoadOperation(new[] { new StorageObjectQuery(StorageId, typeof(string)) },
                    new(new TaskCompletionSource<StorageObject[]>()), CancellationToken.None);
                queue.EnqueueSaveOperation(new[] { new StorageObjectMutation(new(StorageId, "Test", typeof(string))) },
                    new(new TaskCompletionSource<StorageObject[]>()), CancellationToken.None);
                queue.EnqueueDeleteOperation(new[] { new StorageObjectDeletion(StorageId) },
                    new(new TaskCompletionSource<StorageObject[]>()), CancellationToken.None);
            });

            while (!result.IsCompleted)
            {
                await Task.Yield();
            }

            while (!queue.IsEmpty)
            {
                await Task.Yield();
            }
        }

        [Test, TestCase(true, true), TestCase(true, false), TestCase(false, true), TestCase(false, false)]
        public void Full_Mutation_Overrides_Previous_Mutations(bool firstIsPartial, bool secondIsPartial)
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                Mutation(StorageId, firstIsPartial, Item1),
                Mutation(StorageId, secondIsPartial, Item2),
                FullMutation(StorageId, Item3)
            };
            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextDeletions, Is.Empty);
            Assert.That(nextMutations, Has.Count.EqualTo(1));
            var nextMutation = nextMutations.Single();
            Assert.That(nextMutation.ObjectId, Is.EqualTo(StorageId));
            Assert.That(nextMutation, Is.EquivalentTo(new[] { Item3 }));
        }

        [Test]
        public void Full_Deletion_Overrides_Previous_Deletions()
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                PartialDeletion(StorageId, Keys1To3),
                FullDeletion(StorageId)
            };
            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextMutations, Is.Empty);
            Assert.That(nextDeletions, Has.Count.EqualTo(1));
            var nextDeletion = nextDeletions.Single();
            Assert.That(nextDeletion.ObjectId, Is.EqualTo(StorageId));
            Assert.That(nextDeletion.IsPartial, Is.False);
        }

        [Test, TestCase(true), TestCase(false)]
        public void Partial_Mutations_Get_Merged_With_Previous_Mutations(bool firstIsPartial)
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                Mutation(StorageId, firstIsPartial, Item1),
                PartialMutation(StorageId, Item2),
                PartialMutation(StorageId, Item3)
            };
            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextDeletions, Is.Empty);
            Assert.That(nextMutations, Has.Count.EqualTo(1));
            var nextMutation = nextMutations.Single();
            Assert.That(nextMutation.ObjectId, Is.EqualTo(StorageId));
            Assert.That(nextMutation, Is.EquivalentTo(Items1To3));
        }

        [Test]
        public void Partial_Deletions_Get_Merged_With_Previous_Partial_Deletions()
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                PartialDeletion(StorageId, Key1),
                PartialDeletion(StorageId, Key2),
                PartialDeletion(StorageId, Key3)
            };
            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextMutations, Is.Empty);
            Assert.That(nextDeletions, Has.Count.EqualTo(1));
            var nextDeletion = nextDeletions.Single();
            Assert.That(nextDeletion.ObjectId, Is.EqualTo(StorageId));
            Assert.That(nextDeletion.Filter, Is.EquivalentTo(Keys1To3));
        }

        [Test]
        public void Partial_Deletions_Following_A_Full_Deletion_Are_Discarded()
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                FullDeletion(StorageId),
                PartialDeletion(StorageId, Key2),
                PartialDeletion(StorageId, Key3)
            };
            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextMutations, Is.Empty);
            Assert.That(nextDeletions, Has.Count.EqualTo(1));
            var nextDeletion = nextDeletions.Single();
            Assert.That(nextDeletion.ObjectId, Is.EqualTo(StorageId));
            Assert.That(nextDeletion.Filter, Is.Empty);
        }

        [Test, TestCase(true), TestCase(false)]
        public void Mutations_Targeting_Different_Objects_Get_Merged(bool isPartial)
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                Mutation(StorageId1, isPartial, Items1To3),
                Mutation(StorageId2, isPartial, Items1To3),
                Mutation(StorageId3, isPartial, Items1To3)
            };
            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextDeletions, Is.Empty);
            Assert.That(nextMutations, Has.Count.EqualTo(3));
            var mutation1 = nextMutations.First();
            var mutation2 = nextMutations.Skip(1).First();
            var mutation3 = nextMutations.Skip(2).Single();
            Assert.That(mutation1.ObjectId, Is.EqualTo(StorageId1));
            Assert.That(mutation2.ObjectId, Is.EqualTo(StorageId2));
            Assert.That(mutation3.ObjectId, Is.EqualTo(StorageId3));
            Assert.That(mutation1, Is.EquivalentTo(Items1To3));
            Assert.That(mutation2, Is.EquivalentTo(Items1To3));
            Assert.That(mutation3, Is.EquivalentTo(Items1To3));
        }

        [Test, TestCase(true), TestCase(false)]
        public void Deletions_Targeting_Different_Objects_Get_Merged(bool isPartial)
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                PartialDeletion(StorageId1, Keys1To3),
                PartialDeletion(StorageId2, Keys1To3),
                PartialDeletion(StorageId3, Keys1To3)
            };
            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextMutations, Is.Empty);
            Assert.That(nextDeletions, Has.Count.EqualTo(3));
            var deletion1 = nextDeletions.First();
            var deletion2 = nextDeletions.Skip(1).First();
            var deletion3 = nextDeletions.Skip(2).Single();
            Assert.That(deletion1.ObjectId, Is.EqualTo(StorageId1));
            Assert.That(deletion2.ObjectId, Is.EqualTo(StorageId2));
            Assert.That(deletion3.ObjectId, Is.EqualTo(StorageId3));
            Assert.That(deletion1, Is.EquivalentTo(Keys1To3));
            Assert.That(deletion2, Is.EquivalentTo(Keys1To3));
            Assert.That(deletion3, Is.EquivalentTo(Keys1To3));
        }

        [Test, TestCase(false, false), TestCase(false, true), TestCase(true, false), TestCase(true, true)]
        public void Mutations_Wait_For_Preceding_Deletions_Targeting_Same_Items_To_Finish(bool deletionIsPartial, bool mutationIsPartial)
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                deletionIsPartial ? PartialDeletion(StorageId1, Key1) : FullDeletion(StorageId1),
                Mutation(StorageId1, deletionIsPartial, Item1)
            };

            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Has.Count.EqualTo(1));
            Assert.That(nextMutations, Is.Empty);
            Assert.That(nextDeletions, Has.Count.EqualTo(1));
        }

        [Test, TestCase(false, false), TestCase(false, true), TestCase(true, false), TestCase(true, true)]
        public void Deletions_Wait_For_Preceding_Mutations_Targeting_Same_Items_To_Finish(bool mutationIsPartial, bool deletionIsPartial)
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                Mutation(StorageId1, deletionIsPartial, Item1),
                deletionIsPartial ? PartialDeletion(StorageId1, Key1) : FullDeletion(StorageId1)
            };

            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Has.Count.EqualTo(1));
            Assert.That(nextDeletions, Is.Empty);
            Assert.That(nextMutations, Has.Count.EqualTo(1));
        }

        [Test, TestCase(false, false), TestCase(false, true), TestCase(true, false), TestCase(true, true)]
        public void Mutations_Do_Not_Wait_For_Preceding_Deletions_Targeting_Different_Objects_To_Finish(bool deletionIsPartial, bool mutationIsPartial)
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                deletionIsPartial ? PartialDeletion(StorageId1, Key1) : FullDeletion(StorageId1),
                Mutation(StorageId2, deletionIsPartial, Item1)
            };

            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextMutations, Has.Count.EqualTo(1));
            Assert.That(nextDeletions, Has.Count.EqualTo(1));
        }

        [Test, TestCase(false, false), TestCase(false, true), TestCase(true, false), TestCase(true, true)]
        public void Deletions_Do_Not_Wait_For_Preceding_Mutations_Targeting_Different_Objects_To_Finish(bool deletionIsPartial, bool mutationIsPartial)
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                Mutation(StorageId1, deletionIsPartial, Item1),
                deletionIsPartial ? PartialDeletion(StorageId2, Key1) : FullDeletion(StorageId2)
            };

            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextMutations, Has.Count.EqualTo(1));
            Assert.That(nextDeletions, Has.Count.EqualTo(1));
        }

        [Test]
        public void Partial_Mutations_Do_Not_Wait_For_Preceding_Partial_Deletions_Targeting_Different_Items_On_Same_Objects_To_Finish()
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                PartialDeletion(StorageId1, Key2),
                PartialMutation(StorageId1, Item1)
            };

            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextMutations, Has.Count.EqualTo(1));
            Assert.That(nextDeletions, Has.Count.EqualTo(1));
        }

        [Test]
        public void Partial_Deletions_Do_Not_Wait_For_Preceding_Partial_Mutations_Targeting_Different_Items_On_Same_Objects_To_Finish()
        {
            var queue = new List<DeferredStorageObjectMutationOrDeletion>
            {
                PartialMutation(StorageId1, Item1),
                PartialDeletion(StorageId1, Key2)
            };

            var (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(nextMutations, Has.Count.EqualTo(1));
            Assert.That(nextDeletions, Has.Count.EqualTo(1));
        }

        [Test]
        public void Partial_Queries_Get_Merged_With_Previous_Partial_Queries_Targeting_Same_Object()
        {
            var queue = new List<DeferredStorageObjectQuery>
            {
                PartialQuery(StorageId, Key1),
                PartialQuery(StorageId, Key2),
                PartialQuery(StorageId, Key3),
                PartialQuery(StorageId, Key1, Key2)
            };

            var queries = GetNextQueries(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(queries, Has.Count.EqualTo(1));
            var query = queries.Single();
            Assert.That(query.ObjectId, Is.EqualTo(StorageId));
            Assert.That(query, Is.EquivalentTo(Keys1To3));
            Assert.That(query.IsPartial, Is.True);
        }

        [Test, TestCase(true), TestCase(false)]
        public void Partial_Queries_Following_A_Full_Query_Targeting_Same_Object_Are_Discarded(bool isPartial)
        {
            var queue = new List<DeferredStorageObjectQuery>
            {
                FullQuery(StorageId),
                PartialQuery(StorageId, Key1),
                PartialQuery(StorageId, Key2),
                PartialQuery(StorageId, Key3)
            };

            var queries = GetNextQueries(queue);

            Assert.That(queue, Is.Empty);
            Assert.That(queries, Has.Count.EqualTo(1));
            var query = queries.Single();
            Assert.That(query.ObjectId, Is.EqualTo(StorageId));
            Assert.That(query, Is.Empty);
            Assert.That(query.IsPartial, Is.False);
        }

        [Test, Description("All DeleteObjectAsync requests should get placed in a queue at least momentarily before being sent, so that multiple requests can be made in a loop, and they will all get batched together.")]
        public void DeleteObjectAsync_Request_Is_Not_Sent_Immediately()
        {
            StorageOperationQueue storageOperationQueue = null;
            Func<CloudStorage, StorageOperationQueue> operationQueueFactory = cloudStorage => storageOperationQueue = new(cloudStorage);
            using var cloudStorage = new FakeCloudStorageBuilder().WithStorageOperationQueue(operationQueueFactory).Build();
            cloudStorage.DeleteObjectAsync(StorageId);
            Assert.That(storageOperationQueue.IsEmpty, Is.False);
        }

        [Test, Description("All SaveObjectAsync requests should get placed in a queue at least momentarily before being sent, so that multiple requests can be made in a loop, and they will all get batched together.")]
        public void SaveObjectAsync_Request_Is_Not_Sent_Immediately()
        {
            StorageOperationQueue storageOperationQueue = null;
            Func<CloudStorage, StorageOperationQueue> operationQueueFactory = cloudStorage => storageOperationQueue = new(cloudStorage);
            using var cloudStorage = new FakeCloudStorageBuilder().WithStorageOperationQueue(operationQueueFactory).Build();
            cloudStorage.SaveObjectAsync(StorageId, Item1);
            Assert.That(storageOperationQueue.IsEmpty, Is.False);
        }

        private static DeferredStorageObjectMutationOrDeletion FullMutation(StorageObjectId storageId, params StorageItem[] items) => Mutation(storageId, false, items);
        private static DeferredStorageObjectMutationOrDeletion PartialMutation(StorageObjectId storageId, params StorageItem[] items) => Mutation(storageId, true, items);
        private static DeferredStorageObjectMutationOrDeletion Mutation(StorageObjectId storageId, bool isPartial, params StorageItem[] items) => DeferredStorageObjectMutationOrDeletion.Mutation(storageId, new(storageId, items), items, isPartial, BoolTaskCompletionSource, CancellationToken.None);
        private DeferredStorageObjectMutationOrDeletion PartialDeletion(StorageObjectId storageId, params Key[] filter) => DeferredStorageObjectMutationOrDeletion.Deletion(storageId, filter, true, BoolTaskCompletionSource, CancellationToken.None);
        private DeferredStorageObjectMutationOrDeletion FullDeletion(StorageObjectId storageId) => DeferredStorageObjectMutationOrDeletion.Deletion(storageId, Array.Empty<Key>(), false, BoolTaskCompletionSource, CancellationToken.None);
        private DeferredStorageObjectQuery PartialQuery(StorageObjectId storageId, params Key[] filter) => new(storageId, typeof(Dictionary<string, string>), filter, true, TaskCompletionHandler, CancellationToken.None);
        private DeferredStorageObjectQuery FullQuery(StorageObjectId storageId) => new(storageId, typeof(Dictionary<string, string>), Array.Empty<Key>(), false, TaskCompletionHandler, CancellationToken.None);
        private static (IEnumerable<StorageObjectMutation>, IEnumerable<StorageObjectDeletion>) GetNextMutationsAndDeletions(List<DeferredStorageObjectMutationOrDeletion> queue) => StorageOperationQueue.GetNextMutationsAndDeletions(queue, BoolTaskCompletionSourceList, BoolTaskCompletionSourceList);
        private IEnumerable<StorageObjectQuery> GetNextQueries(List<DeferredStorageObjectQuery> queue) => StorageOperationQueue.GetNextQueries(queue, TaskCompletionHandlerList);
    }
}

namespace Coherence.Toolkit.Tests
{
    using Moq;
    using NUnit.Framework;
    using UnityEngine;
    using Coherence.Tests;

    public class UniquenessManagerTests : CoherenceTest
    {
        private string mockAssetId = "fooAssetId";

        private Mock<Log.Logger> loggerMock;
        private UniquenessManager uniquenessManager;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            loggerMock = new Mock<Log.Logger>(null, null, null);
            loggerMock.Setup(l => l.With<It.IsAnyType>()).Returns(() => loggerMock.Object);

            uniquenessManager = new UniquenessManager(loggerMock.Object);
        }

        [Test]
        public void Should_ReturnRegisteredUuid_When_RegisteringUuidAndAskingForIt()
        {
            var uniqueId1 = "1";

            uniquenessManager.RegisterUniqueId(uniqueId1);

            var result = uniquenessManager.GetUniqueId();

            Assert.IsTrue(uniqueId1.Equals(result));
        }

        [Test]
        public void Should_NotFindUniqueObjectForUuid_When_NoneExists()
        {
            var uniqueId1 = "1";

            var uniqueObject = uniquenessManager.TryGetUniqueObject(uniqueId1);

            Assert.IsNull(uniqueObject);
        }

        [Test]
        public void Should_NotFindUniqueObjectForUuid_When_OneWasRegisteredButWasntUnique()
        {
            var uniqueId1 = "1";

            var mockSync = new Mock<ICoherenceSync>();
            mockSync.Setup(sync => sync.EntityState).Returns(new NetworkEntityState(default, AuthorityType.Full, false,
                false, mockSync.Object, uniqueId1));

            var result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync.Object, uniqueId1);
            var uniqueObject = uniquenessManager.TryGetUniqueObject(uniqueId1);

            Assert.IsFalse(result);
            Assert.IsNull(uniqueObject);
        }

        [Test]
        public void Should_FindUniqueObjectForUuid_When_OneWasRegistered()
        {
            var uniqueId1 = "1";
            var mockSync = new Mock<ICoherenceSync>();
            mockSync.Setup(sync => sync.EntityState).Returns(new NetworkEntityState(default, AuthorityType.Full, false,
                false, mockSync.Object, uniqueId1));
            mockSync.Setup(sync => sync.IsUnique).Returns(true);

            var result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync.Object, uniqueId1);
            var uniqueObject = uniquenessManager.TryGetUniqueObject(uniqueId1);

            Assert.IsFalse(result);
            Assert.IsTrue(uniqueObject != null);
            Assert.IsTrue(uniqueObject.localObject != null);
        }

        [Test]
        public void Should_SetUniqueObjectReplacement_When_DuplicateEntityIsRemoved()
        {
            var uniqueId1 = "1";

            var mockSync = new Mock<ICoherenceSync>();
            var entity = new NetworkEntityState(default, AuthorityType.Full, false,
                false, mockSync.Object, uniqueId1);
            mockSync.Setup(sync => sync.EntityState).Returns(entity);
            mockSync.Setup(sync => sync.IsUnique).Returns(true);

            var result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync.Object, uniqueId1);
            uniquenessManager.ReplaceRemoteDuplicatedEntity(mockSync.Object, entity);
            var uniqueObject = uniquenessManager.TryGetUniqueObject(uniqueId1);

            Assert.IsFalse(result);
            Assert.IsTrue(uniqueObject != null);
            Assert.IsTrue(uniqueObject.localObject == mockSync.Object);
        }

        [Test]
        public void Should_InitUniqueObjecReplacement_When_NewUniqueNetworkEntitySpawns()
        {
            var uniqueId1 = "1";

            var mockSync = new Mock<ICoherenceSync>();
            var entity = new NetworkEntityState(default, AuthorityType.Full, false,
                false, mockSync.Object, uniqueId1);
            mockSync.Setup(sync => sync.EntityState).Returns(entity);
            mockSync.Setup(sync => sync.IsUnique).Returns(true);
            bool calledInitReplaced = false;
            mockSync.Setup(sync => sync.InitializeReplacedUniqueObject(
                It.IsAny<SpawnInfo>())).Callback<SpawnInfo>(
                (info) =>
                {
                    calledInitReplaced = true;
                });

            var result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync.Object, uniqueId1);
            uniquenessManager.ReplaceRemoteDuplicatedEntity(mockSync.Object, entity);
            uniquenessManager.FindUniqueObjectForNewRemoteNetworkEntity(
                new SpawnInfo() { assetId = mockAssetId, uniqueId = uniqueId1 }, null);

            Assert.IsFalse(result);
            Assert.IsTrue(calledInitReplaced);
        }

        [Test]
        public void Should_NotDestroyAsDuplicate_When_AuthObjectIsCreatedByUserAndUniqueObjectAlreadyExists()
        {
            var uniqueId1 = "1";

            var mockSync = new Mock<ICoherenceSync>();
            var entity = new NetworkEntityState(default, AuthorityType.Full, false,
                false, mockSync.Object, uniqueId1);
            mockSync.Setup(sync => sync.EntityState).Returns(entity);
            mockSync.Setup(sync => sync.IsUnique).Returns(true);
            bool calledDestroyAsDuplicate = false;
            mockSync.Setup(sync => sync.DestroyAsDuplicate()).Callback(
                () =>
                {
                    calledDestroyAsDuplicate = true;
                });

            var result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync.Object, uniqueId1);
            result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync.Object, uniqueId1);

            Assert.IsFalse(result);
            Assert.IsFalse(calledDestroyAsDuplicate);
        }

        [Test]
        public void Should_DestroyAsDuplicate_When_AuthObjectIsCreatedByUserAndSecondUniqueObjectAlreadyExists()
        {
            var uniqueId1 = "1";

            var mockSync = new Mock<ICoherenceSync>();
            var entity = new NetworkEntityState(default, AuthorityType.Full, false,
                false, mockSync.Object, uniqueId1);
            var gameObject = new GameObject();
            mockSync.Setup(sync => sync.EntityState).Returns(entity);
            mockSync.Setup(sync => sync.IsUnique).Returns(true);
            mockSync.Setup(sync => sync.gameObject).Returns(gameObject);

            var mockSync2 = new Mock<ICoherenceSync>();
            var entity2 = new NetworkEntityState(default, AuthorityType.Full, false,
                false, mockSync.Object, uniqueId1);
            mockSync2.Setup(sync => sync.EntityState).Returns(entity2);
            mockSync2.Setup(sync => sync.IsUnique).Returns(true);
            bool calledDestroyAsDuplicate = false;
            mockSync2.Setup(sync => sync.DestroyAsDuplicate()).Callback(
                () =>
                {
                    calledDestroyAsDuplicate = true;
                });

            var result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync.Object, uniqueId1);
            result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync2.Object, uniqueId1);

            Assert.IsTrue(result);
            Assert.IsTrue(calledDestroyAsDuplicate);
        }

        [Test]
        public void Should_DestroyDisabled_When_AuthObjectIsCreatedByUserAndSecondUniqueObjectAlreadyExistButDisabled()
        {
            var uniqueId1 = "1";

            var mockSync = new Mock<ICoherenceSync>();
            var entity = new NetworkEntityState(default, AuthorityType.Full, false,
                false, mockSync.Object, uniqueId1);
            var gameObject = new GameObject();
            gameObject.SetActive(false);
            mockSync.Setup(sync => sync.EntityState).Returns(entity);
            mockSync.Setup(sync => sync.IsUnique).Returns(true);
            mockSync.Setup(sync => sync.gameObject).Returns(gameObject);
            bool calledDestroyAsDuplicate = false;
            mockSync.Setup(sync => sync.DestroyAsDuplicate()).Callback(
                () =>
                {
                    calledDestroyAsDuplicate = true;
                });

            var mockSync2 = new Mock<ICoherenceSync>();
            var entity2 = new NetworkEntityState(default, AuthorityType.Full, false,
                false, mockSync.Object, uniqueId1);
            var gameObject2 = new GameObject();
            mockSync2.Setup(sync => sync.EntityState).Returns(entity2);
            mockSync2.Setup(sync => sync.IsUnique).Returns(true);
            mockSync2.Setup(sync => sync.gameObject).Returns(gameObject2);

            var result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync.Object, uniqueId1);
            result = uniquenessManager.RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(mockSync2.Object, uniqueId1);

            Assert.IsFalse(result);
            Assert.IsTrue(calledDestroyAsDuplicate);
            Assert.IsTrue(gameObject2.activeSelf);
        }
    }
}

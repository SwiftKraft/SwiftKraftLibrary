// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using Coherence.Tests;
    using Moq;
    using NUnit.Framework;
    using Coherence.Log;
    using Coherence.Common;
    using Coherence.Common.Tests;
    using System;
    using Coherence.Core;
    using Coherence.Entities;
    using UnityEngine;
    using System.Collections.Generic;

    public class FloatingOriginManagerTests : CoherenceTest
    {
        private FloatingOriginManager foManager;

        private Mock<IClient> clientMock;
        private Mock<IEntitiesManager> entitiesManagerMock;

        private EntityIDGenerator entityIDGenerator;

        private FloatingOriginShiftArgs? foShiftedParam;
        private FloatingOriginShiftArgs? afterFOShiftedParam;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            clientMock = new Mock<IClient>(MockBehavior.Strict);
            clientMock.Setup(c => c.IsConnected()).Returns(true);

            var clientMockFloatingOrigin = Vector3d.zero;
            clientMock.Setup(c => c.SetFloatingOrigin(It.IsAny<Vector3d>())).Callback<Vector3d>(fo => clientMockFloatingOrigin = fo);
            clientMock.Setup(c => c.GetFloatingOrigin()).Returns(() => clientMockFloatingOrigin);

            entitiesManagerMock = new Mock<IEntitiesManager>(MockBehavior.Strict);
            entitiesManagerMock.Setup(e => e.NetworkEntities).Returns(new List<NetworkEntityState>());

            foManager = new FloatingOriginManager(clientMock.Object, entitiesManagerMock.Object, logger);

            entityIDGenerator = new EntityIDGenerator(Entity.ClientInitialIndex, Entity.MaxID, false, logger);

            foShiftedParam = null;
            afterFOShiftedParam = null;

            foManager.OnFloatingOriginShifted += e => foShiftedParam = e;
            foManager.OnAfterFloatingOriginShifted += e => afterFOShiftedParam = e;
        }

        [TestCase(true)]
        [TestCase(false)]
        [Description("Tests that when client is not connected, setting the floating origin does nothing.")]
        public void SetFloatingOrigin_WhenClientNotConnected_ShouldDoNothing(bool useTranslate)
        {
            clientMock.Setup(c => c.IsConnected()).Returns(false);

            Assert.That(SetFloatingOrigin(new Vector3d(10, 11, 12), useTranslate), Is.False);

            Assert.That(foManager.GetFloatingOrigin(), Is.EqualTo(Vector3d.zero));

            Assert_FONotShifted();
        }

        [TestCase(true)]
        [TestCase(false)]
        [Description("Tests that setting the floating origin should change it and also invoke the callbacks.")]
        public void SetFloatingOrigin_ShouldInvokeCallbacks(bool useTranslate)
        {
            var fo2 = new Vector3d(10, 11, 12);
            var fo3 = new Vector3d(100, 200, 300);

            Assert.That(SetFloatingOrigin(fo2, useTranslate), Is.True);
            Assert_FOShifted(Vector3d.zero, fo2);
            Assert.That(foManager.GetFloatingOrigin(), Is.EqualTo(fo2));

            Assert.That(SetFloatingOrigin(fo3, useTranslate), Is.True);
            Assert_FOShifted(fo2, fo3);
            Assert.That(foManager.GetFloatingOrigin(), Is.EqualTo(fo3));
        }

        [TestCase(true, 0, 0, 0, false, false)]
        [TestCase(true, FloatingOriginManager.FloatingOriginPreciseRange, 0, 0, false, false)]
        [TestCase(true, FloatingOriginManager.FloatingOriginPreciseRange, FloatingOriginManager.FloatingOriginPreciseRange, FloatingOriginManager.FloatingOriginPreciseRange, false, false)]
        [TestCase(true, -FloatingOriginManager.FloatingOriginPreciseRange, -FloatingOriginManager.FloatingOriginPreciseRange, -FloatingOriginManager.FloatingOriginPreciseRange, false, false)]
        [TestCase(true, FloatingOriginManager.FloatingOriginPreciseRange + 1, 0, 0, true, false)]
        [TestCase(true, 0, FloatingOriginManager.FloatingOriginPreciseRange + 1, 0, true, false)]
        [TestCase(true, 0, 0, FloatingOriginManager.FloatingOriginPreciseRange + 1, true, false)]
        [TestCase(true, 0, 0, -FloatingOriginManager.FloatingOriginPreciseRange - 1, true, false)]
        [TestCase(true, FloatingOriginManager.WorldPositionMaxRange, 0, 0, true, false)]
        [TestCase(true, FloatingOriginManager.WorldPositionMaxRange, FloatingOriginManager.WorldPositionMaxRange, FloatingOriginManager.WorldPositionMaxRange, true, false)]
        [TestCase(true, -FloatingOriginManager.WorldPositionMaxRange, -FloatingOriginManager.WorldPositionMaxRange, -FloatingOriginManager.WorldPositionMaxRange, true, false)]
        [TestCase(true, FloatingOriginManager.WorldPositionMaxRange + 1E22, 0, 0, true, true)]
        [TestCase(true, 0, FloatingOriginManager.WorldPositionMaxRange + 1E22, 0, true, true)]
        [TestCase(true, 0, 0, FloatingOriginManager.WorldPositionMaxRange + 1E22, true, true)]
        [TestCase(true, 0, 0, -FloatingOriginManager.WorldPositionMaxRange - 1E22, true, true)]
        [TestCase(false, 0, 0, 0, false, false)]
        [TestCase(false, FloatingOriginManager.FloatingOriginPreciseRange, 0, 0, false, false)]
        [TestCase(false, FloatingOriginManager.FloatingOriginPreciseRange, FloatingOriginManager.FloatingOriginPreciseRange, FloatingOriginManager.FloatingOriginPreciseRange, false, false)]
        [TestCase(false, -FloatingOriginManager.FloatingOriginPreciseRange, -FloatingOriginManager.FloatingOriginPreciseRange, -FloatingOriginManager.FloatingOriginPreciseRange, false, false)]
        [TestCase(false, FloatingOriginManager.FloatingOriginPreciseRange + 1, 0, 0, true, false)]
        [TestCase(false, 0, FloatingOriginManager.FloatingOriginPreciseRange + 1, 0, true, false)]
        [TestCase(false, 0, 0, FloatingOriginManager.FloatingOriginPreciseRange + 1, true, false)]
        [TestCase(false, 0, 0, -FloatingOriginManager.FloatingOriginPreciseRange - 1, true, false)]
        [TestCase(false, FloatingOriginManager.WorldPositionMaxRange, 0, 0, true, false)]
        [TestCase(false, FloatingOriginManager.WorldPositionMaxRange, FloatingOriginManager.WorldPositionMaxRange, FloatingOriginManager.WorldPositionMaxRange, true, false)]
        [TestCase(false, -FloatingOriginManager.WorldPositionMaxRange, -FloatingOriginManager.WorldPositionMaxRange, -FloatingOriginManager.WorldPositionMaxRange, true, false)]
        [TestCase(false, FloatingOriginManager.WorldPositionMaxRange + 1E22, 0, 0, true, true)]
        [TestCase(false, 0, FloatingOriginManager.WorldPositionMaxRange + 1E22, 0, true, true)]
        [TestCase(false, 0, 0, FloatingOriginManager.WorldPositionMaxRange + 1E22, true, true)]
        [TestCase(false, 0, 0, -FloatingOriginManager.WorldPositionMaxRange - 1E22, true, true)]
        [Description("Tests that setting the floating origin which is too big logs a warning or throws an exception.")]
        public void SetFloatingOrigin_WhenImpreciseRange_ShouldWarnOrThrow(bool useTranslate, double foX, double foY, double foZ, bool shouldWarn, bool shouldThrow)
        {
            var fo = new Vector3d(foX, foY, foZ);

            if (shouldThrow)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => SetFloatingOrigin(fo, useTranslate));
                Assert_FONotShifted();
                Assert.That(foManager.GetFloatingOrigin(), Is.EqualTo(Vector3d.zero));
            }
            else
            {
                Assert.That(SetFloatingOrigin(fo, useTranslate), Is.True);
                Assert_FOShifted(Vector3d.zero, fo);
                Assert.That(foManager.GetFloatingOrigin(), Is.EqualTo(fo));
            }

            if (shouldWarn)
            {
                Assert.That(logger.GetCountForWarningID(Warning.ToolkitFloatingOriginOutOfRange), Is.EqualTo(1));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        [Description("Tests that setting the floating origin shifts all shiftable entities and invokes callbacks on them.")]
        public void SetFloatingOrigin_ShouldShiftNetworkEntities(bool useTranslate)
        {
            var oldFo = new Vector3d(100, 200, 300);
            var newFo = new Vector3d(10, 20, 30);
            var delta = newFo - oldFo;

            SetFloatingOrigin(oldFo, useTranslate);

            var entity1 = new EntityMock(entityIDGenerator) { ShouldShift = true, Position = new Vector3(15, 20, 25) };
            var entity2 = new EntityMock(entityIDGenerator) { ShouldShift = false };
            var entity3 = new EntityMock(entityIDGenerator) { ShouldShift = false, IsParented = true };

            entitiesManagerMock.Setup(e => e.NetworkEntities).Returns(new[] { entity1.State, entity2.State, entity3.State });

            SetFloatingOrigin(newFo, useTranslate);

            entity1.Sync.Verify(c => c.ShiftOrigin(delta), Times.Once());
            entity2.Sync.Verify(c => c.ShiftOrigin(It.IsAny<Vector3d>()), Times.Never());
            entity3.Sync.Verify(c => c.ShiftOrigin(It.IsAny<Vector3d>()), Times.Never());

            entity1.Updater.Verify(u => u.TryFlushPosition(It.IsAny<bool>()), Times.Never());
            entity2.Updater.Verify(u => u.TryFlushPosition(true), Times.Once());
            entity3.Updater.Verify(u => u.TryFlushPosition(It.IsAny<bool>()), Times.Never());

            Assert.That(entity1.FloatingOriginShiftedArgs, Is.EquivalentTo(new[] { (entity1.Position - delta.ToUnityVector3(), entity1.Position) }));
            Assert.That(entity2.FloatingOriginShiftedArgs, Is.Empty);
            Assert.That(entity3.FloatingOriginShiftedArgs, Is.Empty);
        }

        private bool SetFloatingOrigin(Vector3d fo, bool useTranslate)
        {
            if (useTranslate)
            {
                return foManager.TranslateFloatingOrigin(fo - foManager.GetFloatingOrigin());
            }

            return foManager.SetFloatingOrigin(fo);
        }

        private void Assert_FONotShifted()
        {
            Assert.That(foShiftedParam, Is.Null);
            Assert.That(afterFOShiftedParam, Is.Null);
        }

        private void Assert_FOShifted(Vector3d oldFO, Vector3d newFO)
        {
            Assert.That(foShiftedParam, Is.Not.Null);
            Assert.That(foShiftedParam.Value.OldOrigin, Is.EqualTo(oldFO));
            Assert.That(foShiftedParam.Value.NewOrigin, Is.EqualTo(newFO));

            Assert.That(afterFOShiftedParam, Is.Not.Null);
            Assert.That(afterFOShiftedParam.Value.OldOrigin, Is.EqualTo(oldFO));
            Assert.That(afterFOShiftedParam.Value.NewOrigin, Is.EqualTo(newFO));
        }

        private class EntityMock
        {
            public Mock<ICoherenceSync> Sync = new Mock<ICoherenceSync>(MockBehavior.Strict);
            public Mock<ICoherenceSyncUpdater> Updater = new Mock<ICoherenceSyncUpdater>(MockBehavior.Strict);
            public NetworkEntityState State;

            public bool ShouldShift;
            public Vector3 Position;
            public bool IsParented;

            public Queue<(Vector3, Vector3)> FloatingOriginShiftedArgs = new Queue<(Vector3, Vector3)>();

            public EntityMock(EntityIDGenerator idGenerator)
            {
                Sync.Setup(s => s.coherencePosition).Returns(() => Position);
                Sync.Setup(s => s.ShouldShift()).Returns(() => ShouldShift);
                Sync.Setup(s => s.ShiftOrigin(It.IsAny<Vector3d>()))
                    .Callback<Vector3d>(d => Position -= d.ToUnityVector3())
                    .Returns(true);
                Sync.Setup(s => s.Updater).Returns(Updater.Object);
                Sync.Setup(s => s.OnFloatingOriginShifted).Returns((a, b) => FloatingOriginShiftedArgs.Enqueue((a, b)));
                Sync.Setup(s => s.HasParentWithCoherenceSync).Returns(() => IsParented);

                Updater.Setup(u => u.TryFlushPosition(It.IsAny<bool>())).Returns(true);

                Assert.That(idGenerator.GetEntity(out var entityId), Is.EqualTo(EntityIDGenerator.Error.None));

                State = new NetworkEntityState(entityId, AuthorityType.Full, false, false, Sync.Object, "");
            }
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_EDITOR

namespace Coherence.Toolkit.Tests
{
    using System;
    using Coherence.Core;
    using Coherence.Entities;
    using Coherence.Toolkit.Bindings;
    using Moq;
    using NUnit.Framework;
    using System.Collections.Generic;
    using UnityEngine;
    using Coherence.Tests;

    public class CommandHandlerTests : CoherenceTest
    {
        class MockComponent : MonoBehaviour
        {
            public void C1(CoherenceSync sync) { }

            public void C2(GameObject go) { }

            public void C3(Transform t) { }

            public void C4(Transform t, string m) { }

            public void C5() { }
        }

        private Mock<Log.Logger> loggerMock;
        private Mock<ICoherenceSync> syncMock;
        private MockBridgeBuilder mockBridgeBuilder;
        private Mock<ICoherenceBridge> bridgeMock;
        private MockComponent mockComponent;

        private CommandsHandler commandsHandler;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            loggerMock = new Mock<Log.Logger>(null, null, null);

            (syncMock, _) = new MockSyncBuilder()
                .Build();

            var coherenceSync = syncMock.Object;

            mockBridgeBuilder = new MockBridgeBuilder().Connected();
            bridgeMock = mockBridgeBuilder.Build();

            syncMock.Setup(sync => sync.CoherenceBridge).Returns(bridgeMock.Object);

            var gameObject = new GameObject();
            mockComponent = gameObject.AddComponent<MockComponent>();

            var command1Descriptor = new Descriptor(typeof(MockComponent), typeof(MockComponent).GetMethod(nameof(MockComponent.C1)));
            var command2Descriptor = new Descriptor(typeof(MockComponent), typeof(MockComponent).GetMethod(nameof(MockComponent.C2)));
            var command3Descriptor = new Descriptor(typeof(MockComponent), typeof(MockComponent).GetMethod(nameof(MockComponent.C3)));
            var command4Descriptor = new Descriptor(typeof(MockComponent), typeof(MockComponent).GetMethod(nameof(MockComponent.C4)));
            var command5Descriptor = new Descriptor(typeof(MockComponent), typeof(MockComponent).GetMethod(nameof(MockComponent.C5)));

            commandsHandler = new CommandsHandler(coherenceSync, new List<Binding>() {
                new CommandBinding(command1Descriptor, mockComponent),
                new CommandBinding(command2Descriptor, mockComponent),
                new CommandBinding(command3Descriptor, mockComponent),
                new CommandBinding(command4Descriptor, mockComponent),
                new CommandBinding(command5Descriptor, mockComponent),
            }, loggerMock.Object);

            commandsHandler.AddBakedCommand("Coherence.Toolkit.Tests.CommandHandlerTests+MockComponent.C1", "(Coherence.Toolkit.CoherenceSync)",
                (a, b, c) => { }, (a, b, c) => { }, MessageTarget.AuthorityOnly, mockComponent, false);
            commandsHandler.AddBakedCommand("Coherence.Toolkit.Tests.CommandHandlerTests+MockComponent.C2", "(UnityEngine.GameObject)",
                (a, b, c) => { }, (a, b, c) => { }, MessageTarget.AuthorityOnly, mockComponent, false);
            commandsHandler.AddBakedCommand("Coherence.Toolkit.Tests.CommandHandlerTests+MockComponent.C3", "(UnityEngine.Transform)",
                (a, b, c) => { }, (a, b, c) => { }, MessageTarget.AuthorityOnly, mockComponent, false);
            commandsHandler.AddBakedCommand("Coherence.Toolkit.Tests.CommandHandlerTests+MockComponent.C4", "(UnityEngine.TransformSystem.String)",
                (a, b, c) => { }, (a, b, c) => { }, MessageTarget.AuthorityOnly, mockComponent, false);
            commandsHandler.AddBakedCommand("Coherence.Toolkit.Tests.CommandHandlerTests+MockComponent.C5", "()",
                (a, b, c) => { }, (a, b, c) => { }, MessageTarget.AuthorityOnly, mockComponent, false);
        }

        [TearDown]
        public override void TearDown()
        {
            mockBridgeBuilder.Dispose();
            base.TearDown();
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SendCommand_ShouldValidateIfEntitiesAreInitialized_WithCoherenceSync(bool initialized, bool usingReflection)
        {
            // Tests that if you try to send a command with a CoherenceSync as an argument
            // that it validates it if it's initialized (has an EntityState)

            // Arrange
            var cs = new GameObject().AddComponent<CoherenceSync>();

            bridgeMock.Setup(bridge => bridge.UnityObjectToEntityId(It.IsAny<CoherenceSync>()))
                .Returns(initialized ? new Entity(1, 0, Entity.Relative) : Entity.InvalidRelative);

            // Act
            var result = commandsHandler.SendCommand(typeof(MockComponent), nameof(MockComponent.C1), MessageTarget.AuthorityOnly, ChannelID.Default, true, cs);

            // Assert
            Assert.AreEqual(initialized, result);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SendCommand_ShouldValidateIfEntitiesAreInitialized_WithGameObject(bool initialized, bool usingReflection)
        {
            // Tests that if you try to send a command with a GameObject as an argument
            // that it validates it if it's initialized (has an EntityState)

            // Arrange
            var cs = new GameObject();

            bridgeMock.Setup(bridge => bridge.UnityObjectToEntityId(It.IsAny<GameObject>()))
                .Returns(initialized ? new Entity(1, 0, Entity.Relative) : Entity.InvalidRelative);

            // Act
            var result = commandsHandler.SendCommand(typeof(MockComponent), nameof(MockComponent.C2), MessageTarget.AuthorityOnly, ChannelID.Default, true, cs);

            // Assert
            Assert.AreEqual(initialized, result);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SendCommand_ShouldValidateIfEntitiesAreInitialized_WithTransform(bool initialized, bool usingReflection)
        {
            // Tests that if you try to send a command with a Transform as an argument
            // that it validates it if it's initialized (has an EntityState)

            // Arrange
            var cs = new GameObject();

            bridgeMock.Setup(bridge => bridge.UnityObjectToEntityId(It.IsAny<Transform>()))
                .Returns(initialized ? new Entity(1, 0, Entity.Relative) : Entity.InvalidRelative);

            // Act
            var result = commandsHandler.SendCommand(typeof(MockComponent), nameof(MockComponent.C3), MessageTarget.AuthorityOnly, ChannelID.Default, true, cs.transform);

            // Assert
            Assert.AreEqual(initialized, result);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SendCommand_ShouldValidateTupleTypes_WhenOnlyTuplesAndCorrect(bool usingReflection)
        {
            // Tests that if you try to send only tuples as a command arguments and
            // types of tuples matches their values that the command is sent

            // Arrange
            var cs = new GameObject().AddComponent<CoherenceSync>();

            bridgeMock.Setup(bridge => bridge.UnityObjectToEntityId(It.IsAny<CoherenceSync>()))
                .Returns(new Entity(1, 0, Entity.Relative));

            // Act
            var result = commandsHandler.SendCommand(typeof(MockComponent), nameof(MockComponent.C1), MessageTarget.AuthorityOnly, ChannelID.Default, true, (typeof(CoherenceSync), cs));

            // Assert
            Assert.IsTrue(result);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SendCommand_ShouldValidateTupleTypes_WhenOnlyTuplesAndIncorrect(bool usingReflection)
        {
            // Tests that if you try to send only tuples as a command arguments and
            // types of tuples DO NOT match their values that the command is NOT sent

            // Arrange
            var cs = new GameObject().AddComponent<CoherenceSync>();

            // Act
            var result = commandsHandler.SendCommand(typeof(MockComponent), nameof(MockComponent.C1), MessageTarget.AuthorityOnly, ChannelID.Default, true, (typeof(Transform), cs));

            // Assert
            Assert.IsFalse(result);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SendCommand_ShouldValidateTupleTypes_WhenMixTuplesObjectsAndCorrect(bool usingReflection)
        {
            // Tests that if you try to send tuples mixed with objects as a command arguments and
            // types of tuples matches their values that the command is sent

            // Arrange
            var cs = new GameObject();

            bridgeMock.Setup(bridge => bridge.UnityObjectToEntityId(It.IsAny<Transform>()))
                .Returns(new Entity(1, 0, Entity.Relative));

            // Act
            var result = commandsHandler.SendCommand(typeof(MockComponent), nameof(MockComponent.C4), MessageTarget.AuthorityOnly, ChannelID.Default, true, (typeof(Transform), cs.transform), "m");

            // Assert
            Assert.IsTrue(result);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SendCommand_ShouldValidateTupleTypes_WhenMixTuplesObjectsAndIncorrect(bool usingReflection)
        {
            // Tests that if you try to send tuples mixed with objects as a command arguments and
            // types of tuples DO NOT match their values that the command is NOT sent

            // Arrange
            var cs = new GameObject();

            bridgeMock.Setup(bridge => bridge.UnityObjectToEntityId(It.IsAny<Transform>()))
                .Returns(new Entity(1, 0, Entity.Relative));

            // Act
            var result = commandsHandler.SendCommand(typeof(MockComponent), nameof(MockComponent.C4), MessageTarget.AuthorityOnly, ChannelID.Default, true, (typeof(Transform), cs), "m");

            // Assert
            Assert.IsFalse(result);
        }

        [TestCase(MessageTarget.AuthorityOnly, true)]
        [TestCase(MessageTarget.AuthorityOnly, false)]
        [Description("SendCommand should work even if there is no CoherenceBridge and the command is targetting all or authority only")]
        public void SendCommand_ShouldSendCommand_WhenOffline(MessageTarget target, bool usingReflection)
        {
            // Arrange
            var go = new GameObject();
            var cs = go.AddComponent<CoherenceSync>();
            go.AddComponent<MockComponent>();
            syncMock.Setup(sync => sync.CoherenceBridge).Returns((ICoherenceBridge)null);
            syncMock.Setup(sync => sync.gameObject).Returns(go);

            // Act
            var result = commandsHandler.SendCommand(typeof(MockComponent), nameof(MockComponent.C1), target, ChannelID.Default, true, cs);

            // Assert
            Assert.That(result, Is.True);
        }

        [TestCase(MessageTarget.Other, true)]
        [TestCase(MessageTarget.Other, false)]
        [Description("SendCommand will not work if there is no CoherenceBridge and the command is targeting other clients")]
        public void SendCommand_ShouldNotSendCommand_When_Offline_And_TargetOther(MessageTarget target, bool usingReflection)
        {
            // Arrange
            var go = new GameObject();
            var cs = go.AddComponent<CoherenceSync>();
            go.AddComponent<MockComponent>();
            syncMock.Setup(sync => sync.CoherenceBridge).Returns((ICoherenceBridge)null);
            syncMock.Setup(sync => sync.gameObject).Returns(go);

            // Act
            var result = commandsHandler.SendCommand(typeof(MockComponent), nameof(MockComponent.C1), target, ChannelID.Default, true, cs);

            // Assert
            Assert.That(result, Is.False);
            loggerMock.Verify(logger => logger.Warning(It.Is<Coherence.Log.Warning>(id => id == Coherence.Log.Warning.ToolkitCommandBridgeDisconnected), It.IsAny<string>()));
        }

        [TestCase(MessageTarget.AuthorityOnly, true)]
        [TestCase(MessageTarget.AuthorityOnly, false)]
        [Description("SendCommand with Action should work even if there is no CoherenceBridge and the command is targetting all or authority only")]
        public void SendCommand_ShouldSendActionCommand_WhenOffline(MessageTarget target, bool usingReflection)
        {
            // Arrange
            var go = new GameObject();
            var cs = go.AddComponent<CoherenceSync>();
            go.AddComponent<MockComponent>();
            syncMock.Setup(sync => sync.CoherenceBridge).Returns((ICoherenceBridge)null);
            syncMock.Setup(sync => sync.gameObject).Returns(go);

            // Act
            var result = commandsHandler.SendCommand(mockComponent.C5, target, ChannelID.Default);

            // Assert
            Assert.That(result, Is.True);
        }

        [TestCase(MessageTarget.AuthorityOnly, true)]
        [TestCase(MessageTarget.AuthorityOnly, false)]
        [Description("SendCommand with Action with parameters should work even if there is no CoherenceBridge and the command is targetting all or authority only")]
        public void SendCommand_ShouldSendActionWithParamsCommand_WhenOffline(MessageTarget target, bool usingReflection)
        {
            // Arrange
            var go = new GameObject();
            var cs = go.AddComponent<CoherenceSync>();
            go.AddComponent<MockComponent>();
            syncMock.Setup(sync => sync.CoherenceBridge).Returns((ICoherenceBridge)null);
            syncMock.Setup(sync => sync.gameObject).Returns(go);

            // Act
            var args = new ValueTuple<Type, object>[]
            {
                new() { Item1 = typeof(CoherenceSync), Item2 = cs },
            };
            var result = commandsHandler.SendCommand<CoherenceSync>(mockComponent.C1, target, ChannelID.Default, args);

            // Assert
            Assert.That(result, Is.True);
        }

        [TestCase(MessageTarget.AuthorityOnly, true)]
        [TestCase(MessageTarget.AuthorityOnly, false)]
        [Description("SendCommand with Action with parameters should work even if there is no CoherenceBridge and the command is targetting all or authority only")]
        public void SendCommand_ShouldSendActionWithMultipleParamsCommand_WhenOffline(MessageTarget target, bool usingReflection)
        {
            // Arrange
            var go = new GameObject();
            var cs = go.AddComponent<CoherenceSync>();
            go.AddComponent<MockComponent>();
            syncMock.Setup(sync => sync.CoherenceBridge).Returns((ICoherenceBridge)null);
            syncMock.Setup(sync => sync.gameObject).Returns(go);

            // Act
            var args = new ValueTuple<Type, object>[]
            {
                new() { Item1 = typeof(Transform), Item2 = go.transform },
                new() { Item1 = typeof(string), Item2 = go.name },
            };
            var result = commandsHandler.SendCommand<Transform, string>(mockComponent.C4, target, ChannelID.Default, args);

            // Assert
            Assert.That(result, Is.True);
        }
    }
}

#endif // UNITY_EDITOR

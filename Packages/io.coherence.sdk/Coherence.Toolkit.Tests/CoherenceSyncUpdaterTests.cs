namespace Coherence.Toolkit.Tests
{
    using Coherence;
    using Entities;
    using ProtocolDef;
    using SimulationFrame;
    using Toolkit;
    using Bindings;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Coherence.Tests;

    public class CoherenceSyncUpdaterTests : CoherenceTest
    {
        private class MockBinding : Binding
        {
            private bool isDirty;
            private bool isStopped;
            private bool isReadyToSample;
            private bool isCurrentlyPredicted;

            public bool calledInterpolation;
            public bool receivedComponentData;
            public bool calledSampleValue;
            public bool calledWriteComponentData;
            public bool calledRemoveOutdatedSamples;

            public override string CoherenceComponentName => "MockComponent";

            public MockBinding(bool isDirty, bool isReadyToSample, bool isCurrentlyPredicted)
            {
                this.isDirty = isDirty;
                this.isReadyToSample = isReadyToSample;
                this.isCurrentlyPredicted = isCurrentlyPredicted;
            }

            public override int GetHashCode()
            {
                throw new System.NotImplementedException();
            }

            public override void IsDirty(AbsoluteSimulationFrame simulationFrame, out bool dirty, out bool justStopped)
            {
                dirty = isDirty;
                justStopped = isStopped;
            }

            public override void MarkAsReadyToSend()
            {
            }

            public override ICoherenceComponentData CreateComponentData()
            {
                return new MockComponentData(0);
            }

            public override void Interpolate(double time)
            {
                calledInterpolation = true;
            }

            public override void SampleValue()
            {
                calledSampleValue = true;
            }

            public override ICoherenceComponentData WriteComponentData(ICoherenceComponentData coherenceComponent, AbsoluteSimulationFrame simFrame)
            {
                calledWriteComponentData = true;
                return base.WriteComponentData(coherenceComponent, simFrame);
            }

            public override void RemoveOutdatedSamples(double time)
            {
                calledRemoveOutdatedSamples = true;
            }

            public override void ReceiveComponentData(ICoherenceComponentData coherenceComponent, AbsoluteSimulationFrame clientFrame, Vector3 floatingOriginDelta)
            {
                receivedComponentData = true;
            }

            internal override bool IsReadyToSample(double currentTime)
            {
                return isReadyToSample;
            }

            public override bool IsCurrentlyPredicted()
            {
                return isCurrentlyPredicted;
            }
        }

        private class MockUnityComponent : MonoBehaviour
        {
        }

        private IDefinition GetDefinition()
        {
            var mockDef = new Mock<IDefinition>();
            mockDef.Setup(d => d.GenerateCoherenceUUIDData(It.IsAny<string>(), It.IsAny<AbsoluteSimulationFrame>())).Returns(new MockComponentData(1));
            return mockDef.Object;
        }

        private string GetComponentName(uint id)
        {
            return "MockComponent";
        }

        [Test]
        public void Should_GetComponentUpdate_When_ComponentIsReadyToSampleAndDirty()
        {
            var binding = new MockBinding(true, true, false);
            var go = new GameObject();
            var component = go.AddComponent<MockUnityComponent>();
            binding.unityComponent = component;
            var bindingsList = new List<Binding>() { binding };
            var mockSync = new Mock<ICoherenceSync>();
            mockSync.Setup(c => c.Bindings).Returns(bindingsList);

            var updater = new CoherenceSyncUpdater(mockSync.Object, null);

            var updates = new List<ICoherenceComponentData>();
            updater.SampleAllBindings();
            updater.GetComponentUpdates(updates);

            Assert.IsTrue(binding.calledSampleValue);
            Assert.IsTrue(updates[0].GetType() == typeof(MockComponentData));
            Assert.IsTrue(updates[0].GetComponentType() == 0);
        }

        [Test]
        public void Should_NotGetComponentUpdate_When_ComponentIsNotReadyToSample()
        {
            var binding = new MockBinding(true, false, false);
            var go = new GameObject();
            var component = go.AddComponent<MockUnityComponent>();
            binding.unityComponent = component;
            var bindingsList = new List<Binding>() { binding };
            var mockSync = new Mock<ICoherenceSync>();
            mockSync.Setup(c => c.Bindings).Returns(bindingsList);

            var updater = new CoherenceSyncUpdater(mockSync.Object, null);

            var updates = new List<ICoherenceComponentData>();
            updater.SampleAllBindings();
            updater.GetComponentUpdates(updates);

            Assert.IsTrue(binding.calledSampleValue);
            Assert.IsTrue(updates.Count == 0);
        }

        [Test]
        public void Should_NotGetComponentUpdate_When_ComponentIsNotDirty()
        {
            var binding = new MockBinding(false, true, false);
            var go = new GameObject();
            var component = go.AddComponent<MockUnityComponent>();
            binding.unityComponent = component;
            var bindingsList = new List<Binding>() { binding };
            var mockSync = new Mock<ICoherenceSync>();
            mockSync.Setup(c => c.Bindings).Returns(bindingsList);

            var updater = new CoherenceSyncUpdater(mockSync.Object, null);

            var updates = new List<ICoherenceComponentData>();
            updater.SampleAllBindings();
            updater.GetComponentUpdates(updates);

            Assert.IsTrue(binding.calledSampleValue);
            Assert.IsTrue(updates.Count == 0);
        }

        [Test]
        public void Should_GetUuidComponent_When_UuidExistsAndIsDifferentThanLastSerialized()
        {
            var binding = new MockBinding(true, true, false);
            var go = new GameObject();
            var component = go.AddComponent<MockUnityComponent>();
            binding.unityComponent = component;
            var bindingsList = new List<Binding>() { binding };
            var mockSync = new Mock<ICoherenceSync>();
            mockSync.Setup(c => c.Bindings).Returns(bindingsList);
            mockSync.Setup(c => c.EntityState)
                .Returns(new NetworkEntityState(default, default, default, default, mockSync.Object, "mockId"));

            Impl.GetRootDefinition = GetDefinition;

            var updater = new CoherenceSyncUpdater(mockSync.Object, null);

            var updates = new List<ICoherenceComponentData>();
            updater.SampleAllBindings();
            updater.GetComponentUpdates(updates);

            Assert.IsTrue(updates[1].GetComponentType() == 1);
        }

        [Test]
        public void Should_CallInterpolation_On_NonPredictedBindings()
        {
            var binding1 = new MockBinding(true, true, false);
            var binding2 = new MockBinding(true, true, false);
            var binding3 = new MockBinding(true, true, true);

            var go = new GameObject();
            var component = go.AddComponent<CoherenceBridge>();
            var mockComponent = go.AddComponent<MockUnityComponent>();
            binding1.unityComponent = mockComponent;
            binding2.unityComponent = mockComponent;
            binding3.unityComponent = mockComponent;
            var bindingsList = new List<Binding>() { binding1, binding2, binding3 };
            var mockSync = new Mock<ICoherenceSync>();
            mockSync.Setup(c => c.Bindings).Returns(bindingsList);
            mockSync.Setup(c => c.CoherenceBridge).Returns(component);

            var updater = new CoherenceSyncUpdater(mockSync.Object, null);

            updater.PerformInterpolationOnAllBindings();

            Assert.IsTrue(binding1.calledInterpolation);
            Assert.IsTrue(binding2.calledInterpolation);
            Assert.IsFalse(binding3.calledInterpolation);
        }

        [Test]
        public void Should_ManuallySendChanges_When_HasAuthority()
        {
            var binding1 = new MockBinding(true, true, false);

            var go = new GameObject();
            var component = go.AddComponent<MockUnityComponent>();
            binding1.unityComponent = component;

            var bindingsList = new List<Binding>() { binding1 };
            var mockSync = new Mock<ICoherenceSync>();
            var expectedEntityId = new Entity(1, Byte.MinValue, Entity.Relative);
            mockSync.Setup(c => c.Bindings).Returns(bindingsList);
            mockSync.Setup(c => c.EntityState)
                .Returns(new NetworkEntityState(expectedEntityId, AuthorityType.Full, default, default, mockSync.Object, null));

            var mockClient = new Mock<IClient>();
            bool sentUpdates = false;
            var receivedEntityId = Entity.InvalidRelative;
            mockClient.Setup(client => client.CanSendUpdates(It.IsAny<Entity>()))
                .Returns(true);
            mockClient.Setup(client => client.UpdateComponents(
                    It.IsAny<Entity>(), It.IsAny<ICoherenceComponentData[]>()))
                .Callback<Entity, ICoherenceComponentData[]>(
                    (s, i) =>
                    {
                        receivedEntityId = s;
                        sentUpdates = true;
                    });

            var updater = new CoherenceSyncUpdater(mockSync.Object, mockClient.Object);
            updater.SampleAllBindings();
            updater.ManuallySendAllChanges(false);

            Assert.IsTrue(sentUpdates);
            Assert.IsTrue(expectedEntityId == receivedEntityId);
        }

        [Test]
        public void Should_NotManuallySendChanges_When_HasNoAuthority()
        {
            var binding1 = new MockBinding(true, true, false);

            var go = new GameObject();
            var component = go.AddComponent<MockUnityComponent>();
            binding1.unityComponent = component;

            var bindingsList = new List<Binding>() { binding1 };
            var mockSync = new Mock<ICoherenceSync>();
            var expectedEntityId = new Entity(1, Byte.MinValue, Entity.Relative);
            mockSync.Setup(c => c.Bindings).Returns(bindingsList);
            mockSync.Setup(c => c.EntityState)
                .Returns(new NetworkEntityState(expectedEntityId, AuthorityType.None, default, default, mockSync.Object, null));

            var mockClient = new Mock<IClient>();
            bool sentUpdates = false;
            var receivedEntityId = Entity.InvalidRelative;
            mockClient.Setup(client => client.UpdateComponents(
                    It.IsAny<Entity>(), It.IsAny<ICoherenceComponentData[]>()))
                .Callback<Entity, ICoherenceComponentData[]>(
                    (s, i) =>
                    {
                        receivedEntityId = s;
                        sentUpdates = true;
                    });

            var updater = new CoherenceSyncUpdater(mockSync.Object, mockClient.Object);
            updater.SampleAllBindings();
            updater.ManuallySendAllChanges(false);

            Assert.IsFalse(sentUpdates);
            Assert.IsFalse(expectedEntityId == receivedEntityId);
        }

        [Test]
        public void Should_ApplyComponentUpdate_When_ComponentUpdateExist()
        {
            var binding = new MockBinding(true, true, false);
            var go = new GameObject();
            var component = go.AddComponent<MockUnityComponent>();
            binding.unityComponent = component;

            var bridge = go.AddComponent<CoherenceBridge>();

            var bindingsList = new List<Binding>() { binding };
            var mockSync = new Mock<ICoherenceSync>();
            mockSync.Setup(c => c.Bindings).Returns(bindingsList);
            mockSync.Setup(c => c.CoherenceBridge).Returns(bridge);

            var updater = new CoherenceSyncUpdater(mockSync.Object, null);

            var updates = new List<ICoherenceComponentData>();
            updater.SampleAllBindings();
            updater.GetComponentUpdates(updates);

            var entityData = ComponentUpdates.New(updates);

            Impl.ComponentNameFromTypeId = GetComponentName;

            updater.ApplyComponentUpdates(entityData);

            Assert.IsTrue(binding.receivedComponentData);
        }
    }
}

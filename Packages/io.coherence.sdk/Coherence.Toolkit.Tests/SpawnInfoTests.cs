// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using System;
    using System.Collections.Generic;
    using Coherence.Entities;
    using Coherence.Generated;
    using Coherence.SimulationFrame;
    using Coherence.Tests;
    using Coherence.Toolkit.Bindings;
    using Moq;
    using Moq.Protected;
    using NUnit.Framework;
    using UnityEngine;

    public class SpawnInfoTests : CoherenceTest
    {
        private SpawnInfo spawnInfo = new();
        private readonly Mock<ICoherenceSync> syncMock = new(MockBehavior.Strict);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            spawnInfo.prefab = syncMock.Object;
        }

        [Test]
        [Description("Tests that when the binding doesn't exist that the exception is thrown.")]
        public void GetBindingValue_WhenCantFindingBinding_ShouldThrow()
        {
            syncMock.Setup(x => x.GetBakedValueBinding<ValueBinding<It.IsAnyType>>(It.IsAny<string>())).Returns(() => null);

            Assert.Throws<Exception>(() => spawnInfo.GetBindingValue<Vector3>("position"));
        }

        [Test]
        [Description("Tests that when the component data for the binding doesn't exist that the exception is thrown.")]
        public void GetBindingValue_WhenCantFindingBindingValue_ShouldThrow()
        {
            spawnInfo.ComponentUpdates = ComponentUpdates.New(0);

            SetupBinding();

            Assert.Throws<Exception>(() => spawnInfo.GetBindingValue<Vector3>("position"));
        }

        [Test]
        [Description("Tests that the binding value is correctly fetched.")]
        public void GetBindingValue_ShouldReturnCorrectValue()
        {
            var bindingValue = new Vector3(10, 11, 12);
            spawnInfo.ComponentUpdates = ComponentUpdates.New(new List<ICoherenceComponentData>() { new WorldPosition() { value = bindingValue } });

            SetupBinding();

            var value = spawnInfo.GetBindingValue<Vector3>("position");

            Assert.That(value, Is.EqualTo(bindingValue));
        }

        private void SetupBinding()
        {
            var binding = new Mock<MockBinding>(MockBehavior.Strict);
            binding.Setup(x => x.CoherenceComponentType).Returns(typeof(WorldPosition));
            binding.Protected().Setup("ReadComponentData", ItExpr.IsAny<ICoherenceComponentData>(), ItExpr.IsAny<Vector3>()).CallBase();

            syncMock.Setup(x => x.GetBakedValueBinding<ValueBinding<Vector3>>(It.IsAny<string>())).Returns(binding.Object);
        }

        public abstract class MockBinding : ValueBinding<Vector3>
        {
            protected override (Vector3 value, AbsoluteSimulationFrame simFrame) ReadComponentData(ICoherenceComponentData coherenceComponent, Vector3 floatingOriginDelta)
            {
                var worldPositionComponent = (WorldPosition)coherenceComponent;
                return (worldPositionComponent.value, worldPositionComponent.valueSimulationFrame);
            }
        }
    }
}

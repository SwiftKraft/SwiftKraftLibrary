// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entity.Tests
{
    using Coherence.SimulationFrame;
    using Entities;
    using Generated;
    using NUnit.Framework;
    using UnityEngine;
    using Coherence.Tests;

    public class DeltaComponentsTests : CoherenceTest
    {
        [Test]
        [Description("Verifies that component is removed when remove is merged")]
        public void Merge_RemovesComponent()
        {
            // Arrange
            DeltaComponents components = new DeltaComponentsBuilder()
                .AddGlobal()
                .Build();

            DeltaComponents remove = new DeltaComponentsBuilder()
                .RemoveGlobal()
                .Build();

            // Act
            components.Merge(remove);

            // Assert
            Assert.That(components.HasComponent(Definition.InternalGlobal), Is.False);
        }

        [Test]
        [Description("Verifies that component is appended when update is merged")]
        public void Merge_AddsComponent()
        {
            // Arrange
            DeltaComponents components = new DeltaComponentsBuilder()
                .AddGlobal()
                .Build();

            DeltaComponents update = new DeltaComponentsBuilder()
                .AddPosition()
                .Build();

            // Act
            components.Merge(update);

            // Assert
            Assert.That(components.HasComponent(Definition.InternalGlobal), Is.True);
            Assert.That(components.HasComponent(Definition.InternalWorldPosition), Is.True);
        }

        [Test]
        [Description("Verifies that component's value is updated when update is merged")]
        public void Merge_UpdatesValue()
        {
            // Arrange
            DeltaComponents components = new DeltaComponentsBuilder()
                .AddPosition(Vector3.zero)
                .Build();

            Vector3 updatedPosition = Vector3.one;

            DeltaComponents update = new DeltaComponentsBuilder()
                .AddPosition(updatedPosition)
                .Build();

            // Act
            components.Merge(update);

            // Assert
            Assert.That(components.TryGetComponent(out WorldPosition worldPosition), Is.True);
            Assert.That(worldPosition.value, Is.EqualTo(updatedPosition));
        }

        [Test]
        [Description("Verifies that pending remove is cleared when update for that component is merged")]
        public void Merge_UpdateClearsRemove()
        {
            // Arrange
            DeltaComponents components = new DeltaComponentsBuilder()
                .RemoveGlobal()
                .Build();

            DeltaComponents update = new DeltaComponentsBuilder()
                .AddGlobal()
                .Build();

            // Act
            components.Merge(update);

            // Assert
            Assert.That(components.HasDestroy(Definition.InternalGlobal), Is.False);
            Assert.That(components.HasComponent(Definition.InternalGlobal));
        }

        [Test]
        [Description("Verifies that component's simFrames is updated when update is merged")]
        public void Merge_UpdatesSimFrames()
        {
            // Arrange
            DeltaComponents components = new DeltaComponentsBuilder()
                .AddSimFramesComponent(
                    intValue: 3, intSimulationFrame: 5,
                    float0Value: 2.2f,
                    float1Value: 3.3f, floatSimulationFrame: 6)
                .Build();

            DeltaComponents update = new DeltaComponentsBuilder()
                .AddSimFramesComponent(
                    float1Value: 4.4f, floatSimulationFrame: 8)
                .Build();

            // Act
            components.Merge(update);

            // Assert
            Assert.That(components.TryGetComponent(out SimFramesComponent component), Is.True);
            Assert.That(component.simFrameIntValue, Is.EqualTo(3));
            Assert.That(component.simFrameIntValueSimulationFrame, Is.EqualTo((AbsoluteSimulationFrame)5));
            Assert.That(component.floatValue, Is.EqualTo(2.2f));
            Assert.That(component.simFrameFloatValue, Is.EqualTo(4.4f));
            Assert.That(component.simFrameFloatValueSimulationFrame, Is.EqualTo((AbsoluteSimulationFrame)8));
        }

        [Test]
        public void GetMinSimulationFrame_Works()
        {
            var component = new SimFramesComponent()
            {
                simFrameIntValueSimulationFrame = 3,
                floatValueSimulationFrame = 1, // ignored since it's not sim-frame enabled field
                simFrameFloatValueSimulationFrame = 2, // skipped because mask is 0
                FieldsMask = SimFramesComponent.simFrameIntValueMask | SimFramesComponent.floatValueMask
            };

            Assert.That(component.GetMinSimulationFrame(), Is.EqualTo((AbsoluteSimulationFrame)3));

            component = new SimFramesComponent()
            {
                simFrameIntValueSimulationFrame = 3,
                floatValueSimulationFrame = 1, // ignored since it's not sim-frame enabled field
                simFrameFloatValueSimulationFrame = 2,
                FieldsMask = SimFramesComponent.simFrameIntValueMask | SimFramesComponent.floatValueMask | SimFramesComponent.simFrameFloatValueMask
            };

            Assert.That(component.GetMinSimulationFrame(), Is.EqualTo((AbsoluteSimulationFrame)2));

            component = new SimFramesComponent()
            {
                simFrameIntValueSimulationFrame = 3, // skipped because mask is 0
                floatValueSimulationFrame = 1, // ignored since it's not sim-frame enabled field
                simFrameFloatValueSimulationFrame = 2, // skipped because mask is 0
                FieldsMask = SimFramesComponent.floatValueMask
            };

            Assert.That(component.GetMinSimulationFrame(), Is.EqualTo(null));
        }
    }
}

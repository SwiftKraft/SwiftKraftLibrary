// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities.Tests
{
    using Coherence.Entity.Tests;
    using Coherence.Generated;
    using Coherence.SimulationFrame;
    using NUnit.Framework;
    using UnityEngine;
    using Coherence.Tests;

    public class MockDefinition : IComponentInfo
    {
        public bool IsSendOrderedComponent(uint componentID)
        {
            return false;
        }
    }

    public class OutgoingEntityUpdateTests : CoherenceTest
    {
        [Description("Verifies that EntityState.Merge outputs correct entity operation in various configurations")]
        [TestCase(EntityOperation.Update, EntityOperation.Create, EntityOperation.Create)]      // Create should be preserved
        [TestCase(EntityOperation.Update, EntityOperation.Update, EntityOperation.Update)]      // Update + Update = Update
        [TestCase(EntityOperation.Create, EntityOperation.Update, EntityOperation.Create)]      // This is possible when dropped create is followed by dropped updates
        [TestCase(EntityOperation.Destroy, EntityOperation.Create, EntityOperation.Destroy)]    // Destroy always takes precedence
        [TestCase(EntityOperation.Destroy, EntityOperation.Update, EntityOperation.Destroy)]    // Destroy always takes precedence
        public void Merge_Works(EntityOperation bufferOperation, EntityOperation droppedOperation, EntityOperation expectedOperation)
        {
            // Arrange
            var inBufferState = new OutgoingEntityUpdate()
            {
                Operation = bufferOperation,
                Components = DeltaComponents.New()
            };

            var droppedState = new OutgoingEntityUpdate()
            {
                Operation = droppedOperation,
                Components = DeltaComponents.New()
            };

            // Act
            droppedState.Add(inBufferState);

            // Assert
            Assert.That(droppedState.Operation, Is.EqualTo(expectedOperation));
        }

        [Test]
        [Description("Tests that clone OutgoingEntityUpdate has exact same values as the original. " +
            "From Operation and Priority, all the way down to field values.")]
        public void Clone_ReturnsExactValues()
        {
            // Arrange
            var operation = EntityOperation.Update;
            var priority = 5533;
            var position = new Vector3(1, 1, 1);
            var intVal = 44;
            var intSimFrame = (AbsoluteSimulationFrame)46;
            var floatVal = 35.35f;

            var original = new OutgoingEntityUpdate()
            {
                Operation = operation,
                Priority = priority,
                Components = new DeltaComponentsBuilder()
                    .AddPosition(position)
                    .AddSimFramesComponent(intVal, intSimFrame, floatVal)
                    .RemoveGlobal()
                    .Build()
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.That(clone.Operation, Is.EqualTo(operation));
            Assert.That(clone.Priority, Is.EqualTo(priority));
            Assert.That(clone.Components.Updates.Count, Is.EqualTo(2));

            Assert.True(clone.Components.TryGetComponent(out WorldPosition cloneWorldPosition));
            Assert.That(cloneWorldPosition.value, Is.EqualTo(position));

            Assert.True(clone.Components.TryGetComponent(out SimFramesComponent cloneSimFrames));
            Assert.That(cloneSimFrames.simFrameIntValue, Is.EqualTo(intVal));
            Assert.That(cloneSimFrames.simFrameIntValueSimulationFrame, Is.EqualTo(intSimFrame));
            Assert.That(cloneSimFrames.floatValue, Is.EqualTo(floatVal));

            Assert.That(clone.Components.Destroys.Count, Is.EqualTo(1));
            Assert.True(clone.Components.Destroys.Contains(Definition.InternalGlobal));
        }

        [Test]
        [Description("Tests that the cloned OutgoingEntityUpdate doesn't share underlying memory in any layer. " +
            "From Operation and Priority, all the way down to field values.")]
        public void Clone_EditingClone_ShouldNotChangeOriginal()
        {
            // Arrange
            var operation = EntityOperation.Update;
            var priority = 5533;
            var position = new Vector3(12, 13, 14);
            var intVal = 44;
            var intSimFrame = (AbsoluteSimulationFrame)46;
            var floatVal = 35.35f;

            var original = new OutgoingEntityUpdate()
            {
                Operation = operation,
                Priority = priority,
                Components = new DeltaComponentsBuilder()
                    .AddPosition(position)
                    .AddSimFramesComponent(intVal, intSimFrame, floatVal)
                    .RemoveGlobal()
                    .Build()
            };

            // Act - clone and heavily edit it in many ways
            var clone = original.Clone();

            clone.Operation = EntityOperation.Create;
            clone.Priority = 1111;
            clone.Components.UpdateComponent(ComponentChange.New(new WorldPosition() { value = new Vector3(1, 1, 1), FieldsMask = 0b1 }));
            clone.Components.UpdateComponent(ComponentChange.New(new SimFramesComponent()
            {
                simFrameIntValue = 1,
                simFrameFloatValue = 1f,
                floatValue = 1.1f,
                floatValueSimulationFrame = 1,
                simFrameFloatValueSimulationFrame = 11,
                simFrameIntValueSimulationFrame = 111,
                FieldsMask = 0b111,
            }));
            clone.Components.UpdateComponent(ComponentChange.New(new WorldOrientation()));
            clone.Components.Destroys.Add(Definition.InternalGlobalQuery);

            // Assert - that the original stays unchanged
            Assert.That(original.Operation, Is.EqualTo(operation));
            Assert.That(original.Priority, Is.EqualTo(priority));
            Assert.That(original.Components.Updates.Count, Is.EqualTo(2));

            Assert.True(original.Components.TryGetComponent(out WorldPosition originalWorldPosition));
            Assert.That(originalWorldPosition.value, Is.EqualTo(position));

            Assert.True(original.Components.TryGetComponent(out SimFramesComponent originalSimFrames));
            Assert.That(originalSimFrames.simFrameIntValue, Is.EqualTo(intVal));
            Assert.That(originalSimFrames.simFrameIntValueSimulationFrame, Is.EqualTo(intSimFrame));
            Assert.That(originalSimFrames.floatValue, Is.EqualTo(floatVal));

            Assert.That(original.Components.Destroys.Count, Is.EqualTo(1));
            Assert.True(original.Components.Destroys.Contains(Definition.InternalGlobal));
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using Common;
    using Entities;
    using Entity.Tests;
    using Generated;
    using Log;
    using Moq;
    using NUnit.Framework;
    using ProtocolDef;
    using Coherence.Tests;

    public class ReceiveChangeBufferTests : CoherenceTest
    {
        private ReceiveChangeBuffer changeBuffer;
        private Mock<IDateTimeProvider> dateTimeProviderMock;
        private Mock<IEntityRegistry> entityRegistryMock;
        private List<IncomingEntityUpdate> updates;
        private List<IEntityMessage> commands;
        private List<IEntityMessage> inputs;

        private Entity entity1 = new Entity(101, 0, false);
        private Entity entity2 = new Entity(102, 0, false);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            entityRegistryMock = new Mock<IEntityRegistry>();
            dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(() => DateTime.UtcNow);

            changeBuffer = new ReceiveChangeBuffer(entityRegistryMock.Object, logger,
                dateTimeProviderMock.Object);

            updates = new List<IncomingEntityUpdate>();
            commands = new List<IEntityMessage>();
            inputs = new List<IEntityMessage>();
        }

        [Test]
        [Description("Verifies that the TryPushUpdatesInto doesn't return a partial update - " +
                     "an update that was added to the buffer before a create for this entity.")]
        public void UpdateIsHeldBackIfTheresNoCreate()
        {
            // Arrange
            var partialUpdate = new IncomingEntityUpdateBuilder(EntityOperation.Update)
                .Components(c => c.AddGlobal())
                .Build();

            // Act
            changeBuffer.AddChange(partialUpdate);
            TakeUpdates(updates, IncomingEntityUpdateBuilder.DefaultEntity);

            // Assert
            Assert.That(updates, Is.Empty);
        }

        [Test]
        [Description("Verifies that the TryPushUpdatesInto returns an update if entity is known.")]
        public void UpdateProcessForKnown()
        {
            // Arrange
            var update = new IncomingEntityUpdateBuilder(EntityOperation.Update)
                .Components(c => c.AddGlobal())
                .Build();

            MakeEntityKnown(IncomingEntityUpdateBuilder.DefaultEntity);

            // Act
            changeBuffer.AddChange(update);
            TakeUpdates(updates, IncomingEntityUpdateBuilder.DefaultEntity);

            // Assert
            Assert.That(updates, Has.Count.EqualTo(1));
            Assert.That(updates[0].Meta.Operation, Is.EqualTo(EntityOperation.Update));
        }

        [Test]
        [Description("Verifies that create on its own is never held back.")]
        public void CreateNeverHeldBack()
        {
            // Arrange
            var update = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Build();

            // Act
            changeBuffer.AddChange(update);
            TakeUpdates(updates, IncomingEntityUpdateBuilder.DefaultEntity);

            // Assert
            Assert.That(updates, Has.Count.EqualTo(1));
            Assert.That(updates[0].Meta.Operation, Is.EqualTo(EntityOperation.Create));
        }

        [Test]
        [Description("Verifies that create that isn't resovable is held back.")]
        public void CreateUnresovableHeldBack()
        {
            // Arrange
            var update = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Build();

            // Act
            changeBuffer.AddChange(update);
            TakeUpdates(updates);

            // Assert
            Assert.That(updates, Is.Empty);
        }

        [Test]
        [Description("Verifies that destroy on its own is never held back.")]
        public void DestroyNeverHeldBack()
        {
            // Arrange
            var update = new IncomingEntityUpdateBuilder(EntityOperation.Destroy)
                .Build();

            MakeEntityKnown(update.Entity);

            // Act
            changeBuffer.AddChange(update);
            TakeUpdates(updates);

            // Assert
            Assert.That(updates, Has.Count.EqualTo(1));
            Assert.That(updates[0].Meta.Operation, Is.EqualTo(EntityOperation.Destroy));
        }

        [Test]
        [Description("Verifies that if we add an update followed by a create " +
                     "we can take both from the buffer (they are not held back).")]
        public void CreateAddedAfterUpdateCanBeTaken()
        {
            // Arrange
            var partialUpdate = new IncomingEntityUpdateBuilder(EntityOperation.Update)
                .Components(c => c.AddGlobal())
                .Build();

            var create = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Components(c => c.AddPosition())
                .Build();

            // Act
            changeBuffer.AddChange(partialUpdate);
            changeBuffer.AddChange(create);
            TakeUpdates(updates, IncomingEntityUpdateBuilder.DefaultEntity);

            // Assert
            Assert.That(updates, Has.Count.EqualTo(1));
            Assert.That(updates[0].Components.HasComponent(Definition.InternalGlobal));
            Assert.That(updates[0].Components.HasComponent(Definition.InternalWorldPosition));
        }

        [Test]
        [Description("Verifies that if we add a create followed by an update " +
                     "we can take both from the buffer (they are not held back).")]
        public void UpdateAddedAfterCreateCanBeTaken()
        {
            // Arrange
            var create = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Components(c => c.AddPosition())
                .Build();

            var update = new IncomingEntityUpdateBuilder(EntityOperation.Update)
                .Components(c => c.AddGlobal())
                .Build();

            // Act
            changeBuffer.AddChange(create);
            changeBuffer.AddChange(update);
            TakeUpdates(updates, IncomingEntityUpdateBuilder.DefaultEntity);

            // Assert
            Assert.That(updates, Has.Count.EqualTo(1));
            Assert.That(updates[0].Components.HasComponent(Definition.InternalGlobal));
            Assert.That(updates[0].Components.HasComponent(Definition.InternalWorldPosition));
        }

        [Test]
        [Description("Verifies that if buffer receives a create request with component A, " +
                     "followed by a remove request for A, followed by resent create but " +
                     "with recreated component A, the merged result contains component A.")]
        public void RemoveFollowedByCreateMergesCorrectly()
        {
            // Arrange
            var create = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Components(c => c.AddGlobal())
                .Build();

            var remove = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Components(c => c.RemoveGlobal())
                .Build();

            var resentCreate = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Components(c => c.AddGlobal())
                .Build();

            // Act
            changeBuffer.AddChange(create);
            changeBuffer.AddChange(remove);
            changeBuffer.AddChange(resentCreate);
            TakeUpdates(updates, IncomingEntityUpdateBuilder.DefaultEntity);

            // Assert
            Assert.That(updates, Has.Count.EqualTo(1));
            Assert.That(updates[0].Components.HasComponent(Definition.InternalGlobal));
        }

        [Test]
        [Description("Verifies that if buffer receives a destroy entity request " +
                     "while there's a pending create request for this entity, " +
                     "both are simply removed.")]
        public void DestroyMergedOnTopOfCreate()
        {
            // Arrange
            var create = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Build();

            var destroy = new IncomingEntityUpdateBuilder(EntityOperation.Destroy)
                .Build();

            // Act
            changeBuffer.AddChange(create);
            changeBuffer.AddChange(destroy);
            TakeUpdates(updates, IncomingEntityUpdateBuilder.DefaultEntity);

            // Assert
            Assert.That(updates, Is.Empty);
        }

        [Test]
        [Description("Verifies that commands and inputs targeting non-existing entity " +
                     "are held in the buffer until that entity arrives.")]
        public void HoldsOnToCommandsAndInputsForUnknownEntities()
        {
            // Arrange
            changeBuffer.AddCommand(new TestMessage(IncomingEntityUpdateBuilder.DefaultEntity));
            changeBuffer.AddInput(new BoolInput() { Entity = IncomingEntityUpdateBuilder.DefaultEntity });

            // Act
            TakeCommands(commands);
            TakeInputs(inputs);

            // Assert
            Assert.That(commands, Is.Empty);
            Assert.That(inputs, Is.Empty);
        }

        [Test]
        [Description("Verifies that commands referencing unknown entities are held in the buffer " +
                     "until that entity arrives.")]
        public void HoldsOnToCommandsReferencingUnknownEntities()
        {
            // Arrange
            MakeEntityKnown(IncomingEntityUpdateBuilder.DefaultEntity);
            MakeEntityKnown(entity2);

            changeBuffer.AddCommand(new EntityRefsCommand(IncomingEntityUpdateBuilder.DefaultEntity, entity1, entity2));

            // Act
            TakeCommands(commands);

            // Assert
            Assert.That(commands, Is.Empty);

            // Arrange - make entity1 known
            MakeEntityKnown(entity1);

            // Act
            TakeCommands(commands);

            // Assert
            Assert.That(commands, Has.Count.EqualTo(1));
        }

        [Test]
        [Description("Verifies that commands and inputs targeting existing entity " +
                     "can be taken from the buffer.")]
        public void PassCommandsAndInputsForKnownEntities()
        {
            // Arrange
            MakeEntityKnown(IncomingEntityUpdateBuilder.DefaultEntity);

            changeBuffer.AddCommand(new TestMessage(IncomingEntityUpdateBuilder.DefaultEntity));
            changeBuffer.AddInput(new TestMessage(IncomingEntityUpdateBuilder.DefaultEntity));

            // Act
            TakeCommands(commands);
            TakeInputs(inputs);

            // Assert
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(inputs, Has.Count.EqualTo(1));
        }

        [Test]
        [Description("Verifies that inputs and commands that reference an entity which was created in the same tick " +
            "are taken from the buffer.")]
        public void PassCommandsAndInputsForJustCreatedEntities()
        {
            // Arrange
            var entity = new Entity(10, 0, false);

            changeBuffer.AddCommand(new TestMessage(entity));
            changeBuffer.AddInput(new TestMessage(entity));

            // Act
            TakeCommands(commands, entity);
            TakeInputs(inputs, entity);

            // Assert
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(inputs, Has.Count.EqualTo(1));
        }

        [Test]
        [Description("Verifies that inputs and commands that reference remotely created then locally destroyed entity " +
            "are held back.")]
        public void HoldsOnToCommandsReferencingRemotelyCreatedButLocallyDestroyedEntity()
        {
            // Arrange - remotely created
            var entity = new Entity(10, 0, false);

            var create = new IncomingEntityUpdateBuilder(EntityOperation.Create, entity).Build();

            changeBuffer.AddChange(create);

            TakeUpdates(updates, entity);

            MakeEntityKnown(entity);

            changeBuffer.AddCommand(new TestMessage(entity));
            changeBuffer.AddInput(new TestMessage(entity));

            // Act
            TakeCommands(commands);
            TakeInputs(inputs);

            // Assert
            Assert.That(commands, Has.Count.EqualTo(1));
            Assert.That(inputs, Has.Count.EqualTo(1));

            // Clear
            updates.Clear();
            commands.Clear();
            inputs.Clear();

            // Arrange - locally destroyed
            MakeEntityUnknown(entity);

            changeBuffer.AddCommand(new TestMessage(entity));
            changeBuffer.AddInput(new TestMessage(entity));

            // Act
            TakeCommands(commands);
            TakeInputs(inputs);

            // Assert
            Assert.That(commands, Is.Empty);
            Assert.That(inputs, Is.Empty);
        }

        [Test]
        [Description("Verifies that commands and inputs are dropped " +
                     "when a destroy request is added to the buffer.")]
        public void CommandsAndInputsDroppedWhenDestroyArrives()
        {
            // Arrange
            MakeEntityKnown(IncomingEntityUpdateBuilder.DefaultEntity);

            var destroy = new IncomingEntityUpdateBuilder(EntityOperation.Destroy)
                .Build();

            // Act
            changeBuffer.AddCommand(new TestMessage(IncomingEntityUpdateBuilder.DefaultEntity));
            changeBuffer.AddInput(new BoolInput() { Entity = IncomingEntityUpdateBuilder.DefaultEntity });
            changeBuffer.AddChange(destroy);

            TakeCommands(commands, IncomingEntityUpdateBuilder.DefaultEntity);
            TakeInputs(inputs, IncomingEntityUpdateBuilder.DefaultEntity);

            // Assert
            Assert.That(commands, Is.Empty);
            Assert.That(inputs, Is.Empty);
        }

        [Test]
        [Description("Verifies that commands entity fields are nulled when " +
            "referenced entity destroy request is added to the buffer.")]
        public void CommandsEntityRefsNulledWhenDestroyArrivesForReferencedEntity()
        {
            // Arrange
            MakeEntityKnown(IncomingEntityUpdateBuilder.DefaultEntity);
            MakeEntityKnown(entity1);
            MakeEntityKnown(entity2);

            var destroy = new IncomingEntityUpdateBuilder(EntityOperation.Destroy, entity1)
                .Build();

            // Act
            changeBuffer.AddCommand(new EntityRefsCommand(IncomingEntityUpdateBuilder.DefaultEntity, entity1, entity2));
            changeBuffer.AddChange(destroy);

            TakeCommands(commands);

            // Assert
            Assert.That(commands, Has.Count.EqualTo(1));

            var command = (EntityRefsCommand)commands[0];
            Assert.That(command.entityRef1, Is.EqualTo(Entity.InvalidRelative));
        }

        [Test]
        [Description("Verifies that a change that contains a reference to an entity " +
                     "that is not known is held (can't be taken).")]
        public void ChangeIsHeldIfItHasUnresolvedReferences()
        {
            // Arrange
            MakeEntityKnown(IncomingEntityUpdateBuilder.DefaultEntity);
            var unknownEntity = new Entity(20, 0, false);

            var create = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Components(c => c.AddConnectedEntity(unknownEntity))
                .Build();

            // Act
            changeBuffer.AddChange(create);
            TakeUpdates(updates);

            // Assert
            Assert.That(updates, Is.Empty);
        }

        [Test]
        [Description("Verifies that a change that contains a reference to an entity can be taken " +
                     "from the buffer once that entity reference gets resolved (is mapped).")]
        public void ChangeIsReleasedWhenReferencesGetResolved()
        {
            // Arrange
            var referencedEntity = new Entity(20, 0, false);

            var create = new IncomingEntityUpdateBuilder(EntityOperation.Create)
                .Components(c => c.AddConnectedEntity(referencedEntity))
                .Build();

            var referencedCreate = new IncomingEntityUpdateBuilder(EntityOperation.Create, referencedEntity)
                .Build();

            // Act
            changeBuffer.AddChange(create);
            changeBuffer.AddChange(referencedCreate);
            TakeUpdates(updates, IncomingEntityUpdateBuilder.DefaultEntity, referencedEntity);

            // Assert
            Assert.That(updates, Has.Count.EqualTo(2));
            Assert.That(updates, Has.Exactly(1).Matches<IncomingEntityUpdate>(update => update.Entity == referencedEntity));
            Assert.That(updates,
                Has.Exactly(1).Matches<IncomingEntityUpdate>(update => update.Entity == IncomingEntityUpdateBuilder.DefaultEntity));
        }

        [Test]
        [Description("Verifies that inputs and commands for not existing entities are eventually dropped.")]
        public void InputsAndCommandsExpire()
        {
            // Arrange
            var entityA = new Entity(1, 0, false);

            DateTime now = new DateTime(2000, 1, 1, 12, 1, 1);
            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(now);

            changeBuffer.AddInput(new TestMessage(entityA));
            changeBuffer.AddCommand(new TestMessage(entityA));

            // Act
            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(now + ReceiveChangeBuffer.MessageTTL);

            TakeInputs(inputs);
            TakeCommands(commands);

            // Assert
            MakeEntityKnown(entityA);
            TakeInputs(inputs);
            TakeCommands(commands);

            Assert.That(inputs, Is.Empty);
            Assert.That(commands, Is.Empty);
        }

        [Test]
        [Description(
            "Verifies that inputs and commands for not existing entities are not dropped before reaching TTL.")]
        public void InputsAndCommandsDoNotExpireEarly()
        {
            // Arrange
            var entityA = new Entity(1, 0, false);

            DateTime now = new DateTime(2000, 1, 1, 12, 1, 1);
            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(now);

            changeBuffer.AddInput(new TestMessage(entityA));
            changeBuffer.AddCommand(new TestMessage(entityA));

            // Act
            dateTimeProviderMock.Setup(m => m.UtcNow)
                .Returns(now + ReceiveChangeBuffer.MessageTTL - TimeSpan.FromMilliseconds(1f));

            TakeInputs(inputs);
            TakeCommands(commands);

            // Assert
            MakeEntityKnown(entityA);
            TakeInputs(inputs);
            TakeCommands(commands);

            Assert.That(inputs, Is.Not.Empty);
            Assert.That(commands, Is.Not.Empty);
        }

        [Test]
        [Description("Verifies that changes are correctly sorted by operation and then by ref count.")]
        public void ChangesAreSortedByOperationAndRefCount()
        {
            // Arrange
            var entityA = new Entity(1, 0, false);
            var entityB = new Entity(2, 0, false);
            var entityC = new Entity(3, 0, false);
            var entityD = new Entity(4, 0, false);
            var entityE = new Entity(5, 0, false);
            var entityF = new Entity(6, 0, false);
            var entityG = new Entity(7, 0, false);

            var createA = new IncomingEntityUpdateBuilder(EntityOperation.Create, entityA)
                .Components(c => c.AddGlobal())
                .Build();

            var createB = new IncomingEntityUpdateBuilder(EntityOperation.Create, entityB)
                .Components(c => c.AddGlobal())
                .Build();

            var createC = new IncomingEntityUpdateBuilder(EntityOperation.Create, entityC)
                .Components(c => c.AddGlobal().AddConnectedEntity(entityB))
                .Build();

            var updateD = new IncomingEntityUpdateBuilder(EntityOperation.Update, entityD)
                .Components(c => c.AddGlobal())
                .Build();

            var updateE = new IncomingEntityUpdateBuilder(EntityOperation.Update, entityE)
                .Components(c => c.AddGlobal().AddConnectedEntity(entityA))
                .Build();

            var updateF = new IncomingEntityUpdateBuilder(EntityOperation.Update, entityF)
                .Components(c => c.AddGlobal())
                .Build();

            var destroyG = new IncomingEntityUpdateBuilder(EntityOperation.Destroy, entityG)
                .Build();

            MakeEntityKnown(entityD);
            MakeEntityKnown(entityE);
            MakeEntityKnown(entityF);
            MakeEntityKnown(entityG);

            // Act
            changeBuffer.AddChange(updateE);
            changeBuffer.AddChange(createA);
            changeBuffer.AddChange(createC);
            changeBuffer.AddChange(updateD);
            changeBuffer.AddChange(createB);
            changeBuffer.AddChange(updateF);
            changeBuffer.AddChange(destroyG);

            TakeUpdates(updates, entityA, entityB, entityC, entityD, entityE, entityF);

            // Assert
            Assert.That(updates, Has.Count.EqualTo(7));
            Assert.That(updates[0].Meta.Operation, Is.EqualTo(EntityOperation.Create));
            Assert.That(updates[1].Meta.Operation, Is.EqualTo(EntityOperation.Create));
            Assert.That(updates[2].Meta.Operation, Is.EqualTo(EntityOperation.Create));
            Assert.That(updates[3].Meta.Operation, Is.EqualTo(EntityOperation.Destroy));
            Assert.That(updates[4].Meta.Operation, Is.EqualTo(EntityOperation.Update));
            Assert.That(updates[5].Meta.Operation, Is.EqualTo(EntityOperation.Update));
            Assert.That(updates[6].Meta.Operation, Is.EqualTo(EntityOperation.Update));

            Assert.That(updates[0].Entity, Is.EqualTo(entityA).Or.EqualTo(entityB)); // entityA and entityB can be in any order
            Assert.That(updates[1].Entity, Is.EqualTo(entityA).Or.EqualTo(entityB)); // because they have same ref count
            Assert.That(updates[2].Entity, Is.EqualTo(entityC));
            Assert.That(updates[3].Entity, Is.EqualTo(entityG));
            Assert.That(updates[4].Entity, Is.EqualTo(entityD).Or.EqualTo(entityF)); // entityD and entityF can be in any order
            Assert.That(updates[5].Entity, Is.EqualTo(entityD).Or.EqualTo(entityF)); // because they have same ref count
            Assert.That(updates[6].Entity, Is.EqualTo(entityE));
        }

        private void MakeEntityKnown(Entity entity)
        {
            entityRegistryMock.Setup(m => m.EntityExists(entity)).Returns(true);
        }

        private void MakeEntityUnknown(Entity entity)
        {
            entityRegistryMock.Setup(m => m.EntityExists(entity)).Returns(false);
        }

        private void TakeUpdates(List<IncomingEntityUpdate> buffer, params Entity[] resolvableEntities)
        {
            changeBuffer.TakeUpdates(buffer, new List<Entity>(resolvableEntities));
        }
        private void TakeCommands(List<IEntityMessage> buffer, params Entity[] resolvableEntities)
        {
            changeBuffer.TakeCommands(buffer, new List<Entity>(resolvableEntities));
        }

        private void TakeInputs(List<IEntityMessage> buffer, params Entity[] resolvableEntities)
        {
            changeBuffer.TakeInputs(buffer, new List<Entity>(resolvableEntities));
        }

    }
}

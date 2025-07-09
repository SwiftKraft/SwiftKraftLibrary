// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Brook;
    using Entities;
    using Generated;
    using Log;
    using NUnit.Framework;
    using ProtocolDef;
    using Serializer;
    using SimulationFrame;
    using Coherence.Tests;

    public class ChangeBufferTests : CoherenceTest
    {
        private SendChangeBuffer changeBuffer;
        private AbsoluteSimulationFrame simulationFrame;
        private HashSet<Entity> ackedEntities;
        private Entity entityA;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            changeBuffer = new SendChangeBuffer(new Definition(), logger);
            simulationFrame = 0;
            ackedEntities = new HashSet<Entity>();
            entityA = new Entity(1, 0, Entity.Relative);
        }

        [Test]
        public void CreateEntity_ShouldBuffer()
        {
            // Arrange
            var intComponentValue = 10;
            var expectedState = GetEntityState(priority: SendChangeBuffer.CREATE_PRIORITY,
                operation: EntityOperation.Create, intComponentValue: intComponentValue);

            // Act
            CreateEntity(entityA, intComponentValue: intComponentValue);

            // Assert
            Assert_EntityStateSame(entityA, expectedState);
        }

        [Test]
        public void DroppedCreate_ShouldRebuffer()
        {
            // Arrange
            var intComponentValue = 10;
            var expectedState = GetEntityState(priority: SendChangeBuffer.CREATE_PRIORITY,
                operation: EntityOperation.Create,
                intComponentValue: intComponentValue);

            // Act
            CreateEntity(entityA, intComponentValue: intComponentValue);

            var changesSent = SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            DropChanges(changesSent);

            // Assert
            Assert_EntityStateSame(entityA, expectedState);
        }

        [TestCase(false, false, false)] //Update without create like when updating entity created through auth change
        [TestCase(true, false, true)] //If don't send create, the update shouldn't override the IsCreate state
        [TestCase(true, true, false)] //If send create the IsCreate state is cleared by the update
        public void UpdateState_ShouldBuffer(bool createFirst, bool sendCreate, bool expectIsCreateTrue)
        {
            var intComponentValue = 10;
            var updatePriority = 2342; // random number
            var updateValue = 2;
            var updateData = GetComponentData(intComponentValue: updateValue);
            var updateChange = new EntityUpdateChange
            {
                ID = entityA,
                Data = ComponentUpdates.New(updateData),
                Priority = updatePriority,
            };
            var expectedPriority =
                ((createFirst && !sendCreate) ? SendChangeBuffer.CREATE_PRIORITY : 0) + updatePriority;
            var expectedState = GetEntityState(priority: expectedPriority,
                operation: expectIsCreateTrue ? EntityOperation.Create : EntityOperation.Update,
                intComponentValue: updateValue);

            // Act
            if (createFirst)
            {
                CreateEntity(entityA, intComponentValue: intComponentValue);
            }

            if (sendCreate)
            {
                SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);
            }

            // Update with value of 2
            changeBuffer.UpdateEntity(updateChange);

            // Assert
            Assert_EntityStateSame(entityA, expectedState);
        }

        [Test]
        public void DroppedUpdate_ShouldRebuffer()
        {
            var intComponentValue = 10;
            var updatePriority = 2355; // random value
            var updateValue = 20;
            var updateData = GetComponentData(intComponentValue: updateValue);
            var updateChange = new EntityUpdateChange
            {
                ID = entityA,
                Data = ComponentUpdates.New(updateData),
                Priority = updatePriority,
            };
            var expectedState = GetEntityState(priority: updatePriority, intComponentValue: updateValue);

            // Act
            CreateEntity(entityA, intComponentValue: intComponentValue);

            SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            changeBuffer.UpdateEntity(updateChange);

            var changesSent = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            DropChanges(changesSent);

            // Assert
            Assert_EntityStateSame(entityA, expectedState);
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void DestroyEntity_ShouldBuffer(bool createFirst, bool sendCreate)
        {
            var intComponentValue = 10;
            var expectedState = GetEntityState(operation: EntityOperation.Destroy,
                priority: SendChangeBuffer.DESTROY_PRIORITY);

            // Act
            if (createFirst)
            {
                CreateEntity(entityA, intComponentValue: intComponentValue);
            }

            if (sendCreate)
            {
                SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);
            }

            DestroyEntity(entityA);

            // Assert
            if (sendCreate)
            {
                Assert_EntityStateSame(entityA, expectedState);
            }
            else
            {
                Assert.IsFalse(changeBuffer.Buffer.ContainsKey(entityA), "Have Entity in Buffer");
            }
        }

        [Test]
        public void DestroyEntity_FromTransfer()
        {
            var expectedState = GetEntityState(priority: SendChangeBuffer.DESTROY_PRIORITY);
            expectedState.Operation = EntityOperation.Destroy;

            ackedEntities = new HashSet<Entity>
            {
                entityA
            }; // Created by a transfer

            // Act
            DestroyEntity(entityA);

            // Assert
            Assert_EntityStateSame(entityA, expectedState);
        }

        [Test]
        public void DroppedDestroy_ShouldRebuffer()
        {
            var intComponentValue = 10;
            var expectedState = GetEntityState(operation: EntityOperation.Destroy,
                priority: SendChangeBuffer.DESTROY_PRIORITY);

            // Act
            CreateEntity(entityA, intComponentValue: intComponentValue);

            SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            DestroyEntity(entityA);

            var changesSent = SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            DropChanges(changesSent);

            // Assert
            Assert_EntityStateSame(entityA, expectedState);
        }

        [Test]
        [Description("Tests that a destroy is sent even though a create was dropped because a partial update" +
            "could be held on the receiving client.")]
        public void DroppedCreateAfterDestroy()
        {
            CreateEntity(entityA, intComponentValue: 10);
            var sentCreate = SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            DestroyEntity(entityA);

            DropChanges(sentCreate);

            var sentChanges = SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            Assert.That(sentChanges, Has.Count.EqualTo(1));
            Assert.That(sentChanges[entityA].Operation, Is.EqualTo(EntityOperation.Destroy));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RemoveComponent_ShouldBuffer(bool sendCreate)
        {
            var intComponentValue = 10;
            var floatComponentValue = 10.5f;
            var removeChange = new EntityRemoveChange
            {
                ID = entityA,
                Remove = new[]
                {
                    Definition.InternalFloatComponent
                },
            };

            OutgoingEntityUpdate expectedUpdate;
            if (sendCreate)
            {
                expectedUpdate = GetEntityState(destroys: new HashSet<uint>
                {
                    Definition.InternalFloatComponent
                });
            }
            else
            {
                expectedUpdate = GetEntityState(priority: SendChangeBuffer.CREATE_PRIORITY,
                    operation: EntityOperation.Create,
                    intComponentValue: intComponentValue);
            }

            // Act
            CreateEntity(entityA, intComponentValue: intComponentValue, floatComponentValue: floatComponentValue);

            if (sendCreate)
            {
                SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);
            }

            changeBuffer.RemoveComponent(removeChange);

            // Assert
            Assert_EntityStateSame(entityA, expectedUpdate);
        }

        [Test]
        public void DroppedRemoveComponent_ShouldRebuffer()
        {
            var intComponentValue = 10;
            var floatComponentValue = 10.5f;
            var removeChange = new EntityRemoveChange
            {
                ID = entityA,
                Remove = new[]
                {
                    Definition.InternalFloatComponent
                },
            };

            var expectedState = GetEntityState(destroys: new HashSet<uint>
            {
                Definition.InternalFloatComponent
            });

            // Act
            CreateEntity(entityA, intComponentValue: intComponentValue, floatComponentValue: floatComponentValue);

            SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            changeBuffer.RemoveComponent(removeChange);

            var changesSent = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            DropChanges(changesSent);

            // Assert
            Assert_EntityStateSame(entityA, expectedState);
        }

        [Test]
        [Description(
            "Verifies that when component removal is sent, followed by an update being sent for this component," +
            "if removal is dropped, it doesn't get remerged back due to update being more recent.")]
        public void RemovedNotRemergedOnDropIfUpdateWasSent()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // Act
            changeBuffer.RemoveComponent(new EntityRemoveChange()
            {
                ID = entity,
                Remove = new uint[]
                {
                    Definition.InternalIntComponent
                }
            });
            var sentRemove = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            UpdateEntity(entity, intComponentValue: 3);
            var sentUpdate = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            DropChanges(sentRemove, sentUpdate);

            // Assert
            var outgoingEntityUpdate = changeBuffer.CopyEntityUpdate(entity).Value;

            Assert.That(outgoingEntityUpdate.Components.Destroys, Is.Empty);
            Assert.That(outgoingEntityUpdate.Components.Updates.Count, Is.Zero);
        }

        [Test]
        [Description(
            "Verifies that when component removal is sent, followed by an update added to the buffer for this component," +
            "if removal is dropped, it doesn't get remerged back due to update being more recent.")]
        public void RemovedNotRemergedOnDropIfUpdateIsInTheBuffer()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // Act
            changeBuffer.RemoveComponent(new EntityRemoveChange()
            {
                ID = entity,
                Remove = new uint[]
                {
                    Definition.InternalIntComponent
                }
            });
            var sentRemove = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            UpdateEntity(entity, intComponentValue: 3);

            DropChanges(sentRemove);

            // Assert
            var outgoingEntityUpdate = changeBuffer.CopyEntityUpdate(entity).Value;

            Assert.That(outgoingEntityUpdate.Components.Destroys, Is.Empty);
            Assert.That(outgoingEntityUpdate.Components.Updates.Store.ContainsKey(Definition.InternalIntComponent));
        }

        [Test]
        [Description(
            "Verifies that when component removal is sent, followed by another same remove being sent for this component," +
            "if removal is dropped, it doesn't get remerged back due to remove for same component being in-flight.")]
        public void RemovedNotRemergedOnDropIfAnotherRemoveWasSent()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // Act
            changeBuffer.RemoveComponent(new EntityRemoveChange()
            {
                ID = entity,
                Remove = new uint[]
                {
                    Definition.InternalIntComponent
                }
            });
            var sentRemove = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            changeBuffer.RemoveComponent(new EntityRemoveChange()
            {
                ID = entity,
                Remove = new uint[]
                {
                    Definition.InternalIntComponent
                }
            });
            var sentUpdate = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            DropChanges(sentRemove, sentUpdate);

            // Assert
            var outgoingEntityUpdate = changeBuffer.CopyEntityUpdate(entity).Value;

            Assert.That(outgoingEntityUpdate.Components.Destroys, Is.Empty);
            Assert.That(outgoingEntityUpdate.Components.Updates.Count, Is.Zero);
        }

        [Test]
        public void Test_ResendingPreservesOperation()
        {
            // Verifies that when both create and update gets dropped,
            // when both are re-merged into the change buffer, the create operation is preserved.

            // Arrange
            // Add and send a creation change
            var entity = new Entity(1, 0, false);
            CreateEntity(entity);

            var createChanges = SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            // Add and send an update
            UpdateEntity(entity);

            var updateChanges = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            // Act
            DropChanges(createChanges);
            DropChanges(updateChanges);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Operation, Is.EqualTo(EntityOperation.Create));
        }

        [Test]
        public void Test_DroppedSendMergesCorrectly()
        {
            // Verifies that when a dropped create is remerged that the
            // component is correct.

            // Arrange
            // Add and send a creation change
            var createVal = 123;
            var updateVal = 321;

            var entity = new Entity(1, 0, false);
            CreateEntity(entity, intComponentValue: createVal);

            var createChanges = SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            // Add and send an update
            UpdateEntity(entity, intComponentValue: updateVal);

            // Act
            DropChanges(createChanges);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Operation, Is.EqualTo(EntityOperation.Create));
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalIntComponent));

            var intComp = (IntComponent)entityState.Components.Updates.Store[Definition.InternalIntComponent].Data;
            Assert.That(intComp.number, Is.EqualTo(updateVal));
        }

        [Test]
        public void Test_UpdateMergesOverDroppedSendCorrectly()
        {
            // Verifies that when an update merges over a dropped
            // and remerged send that the component is correct.

            // Arrange
            // Add and send a creation change
            var createIntVal = 123;
            var createFloatVal = 0.999f;
            var updateVal = 0.4f;

            var entity = new Entity(1, 0, false);
            CreateEntity(entity, intComponentValue: createIntVal, floatComponentValue: createFloatVal);

            var createChanges = SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            // Add and send an update
            UpdateEntity(entity, floatComponentValue: updateVal);

            var updateChanges = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            // Act
            DropChanges(createChanges, updateChanges);
            DropChanges(updateChanges);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Operation, Is.EqualTo(EntityOperation.Create));
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalIntComponent));

            var intComp = (IntComponent)entityState.Components.Updates.Store[Definition.InternalIntComponent].Data;
            Assert.That(intComp.number, Is.EqualTo(createIntVal));
            var floatComp =
                (FloatComponent)entityState.Components.Updates.Store[Definition.InternalFloatComponent].Data;
            Assert.That(floatComp.number, Is.EqualTo(updateVal));
        }

        [Test]
        [Description("Verifies that if a create is dropped followed by updates with changes " +
                     "to the same components as in the create that the create remerges back into " +
                     "the buffer with the complete changes and not devoid of components.")]
        public void Test_DroppedCreateAfterUpdateWithSameComponentsChangedIsNotEmpty()
        {
            // Arrange
            var droppedFloat = 0.5f;
            var droppedInt = 99;
            var updateFloat = 123.456f;
            var updateFloat2 = 333.333f;
            var updateInt = 202;

            var entity = new Entity(1, 0, false);
            CreateEntity(entity, intComponentValue: droppedInt, floatComponentValue: droppedFloat);

            var createChanges = SendChanges(expectedExistenceChanges: 1, expectedUpdateChanges: 0);

            // add an update that changes the float
            UpdateEntity(entity, floatComponentValue: updateFloat);

            var update1 = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            // add an update that changes the int and float
            UpdateEntity(entity, intComponentValue: updateInt, floatComponentValue: updateFloat2);

            var update2 = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            // add an update with a component not in the original
            UpdateEntity(entity, addOrderedComp: true);

            var update3 = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            // Act
            // Drop the original create
            DropChanges(createChanges, update1, update2, update3);

            // Assert
            // The create should contain all the changes.
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];

            Assert.That(entityState.Operation, Is.EqualTo(EntityOperation.Create));
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalFloatComponent));
            var floatComp =
                (FloatComponent)entityState.Components.Updates.Store[Definition.InternalFloatComponent].Data;
            Assert.That(floatComp.number, Is.EqualTo(updateFloat2));

            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalIntComponent));
            var intComp = (IntComponent)entityState.Components.Updates.Store[Definition.InternalIntComponent].Data;
            Assert.That(intComp.number, Is.EqualTo(updateInt));

            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalOrderedComp));
        }

        [Test]
        [Description("Verifies that the stopped mask is set correctly.")]
        public void Test_StoppedComponentMaskIsSet()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // Act - add an entity change with a stopped mask set
            UpdateMultiCompEntity(entity, 1, 2f, 3f, 0b101);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalMultiComponent));
            var multiComp = (MultiComponent)entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data;
            Assert.That(multiComp.StoppedMask, Is.EqualTo(0b101));
        }

        [Test]
        [Description("Verifies that an existing non-stopped compononet merges with a stopped one correctly.")]
        public void Test_StoppedComponentMaskMergesIntoNonStopped()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            UpdateMultiCompEntity(entity, 1, 2f, 3f, 0b000);

            // Act
            UpdateMultiCompEntity(entity, 4, 5f, 6f, 0b010);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalMultiComponent));
            var multiComp = (MultiComponent)entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data;
            Assert.That(multiComp.StoppedMask, Is.EqualTo(0b010));
        }

        [Test]
        [Description("Verifies that when a new sample is added to the change buffer after one with a stopped mask that the " +
            "mask is correctly updated along with the value.")]
        public void Test_StoppedComponentMergedWithNewUnstoppedSample()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // add an entity change with a stopped mask set
            UpdateMultiCompEntity(entity, 1, 2f, 3f, 0b100);

            // Act - add an entity change with the same component restarted
            UpdateMultiCompEntity(entity, 4, 5f, 6f, 0b000);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalMultiComponent));
            var multiComp = (MultiComponent)entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data;
            Assert.That(multiComp.StoppedMask, Is.EqualTo(0b000));
        }

        [Test]
        [Description("Verifies that when a new sample is added to the change buffer after one with a stopped mask that the " +
            "mask is correctly updated along with the value even if some of the fields are unchanged.")]
        public void Test_StoppedComponentMergedWithNewUnstoppedSampleSomeUnchanged()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // add an entity change with a stopped mask set
            UpdateMultiCompEntity(entity, 1, 2f, 3f, 0b101);

            // Act - add an entity change with the same component restarted
            UpdateMultiCompEntity(entity, 4, 5f, null, 0b000);

            // Assert - 0b100 means that the float1 value is still stopped since it was
            // not chaned in the update.
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalMultiComponent));
            var multiComp = (MultiComponent)entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data;
            Assert.That(multiComp.StoppedMask, Is.EqualTo(0b100));
        }

        [Test]
        [Description("Verifies that if an unstopped sample is dropped and a stopped sample is in the buffer when " +
            "the drop is remerged that the stopped mask is correctly set.")]
        public void Test_StoppedInBufferWithDroppedUnstoppedMergedBackIn()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // add an entity change with a stopped mask set
            UpdateMultiCompEntity(entity, 1, 2f, 3f, 0b100);

            var changesSent = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            UpdateMultiCompEntity(entity, 4, 5f, 6f, 0b101);

            // Act
            DropChanges(changesSent);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalMultiComponent));
            var multiComp = (MultiComponent)entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data;
            Assert.That(multiComp.StoppedMask, Is.EqualTo(0b101));
        }

        [Test]
        [Description("verifies that if an unstopped sample is dropped and a stopped sample is in the buffer when " +
            "the drop is remerged that the stopped mask is correctly set even if some of the fields are unchanged.")]
        public void Test_StoppedInBufferWithDroppedUnstoppedMergedBackInSomeUnchanged()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // add an entity change with a stopped mask set
            UpdateMultiCompEntity(entity, 1, 2f, 3f, 0b100);

            var changesSent = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            UpdateMultiCompEntity(entity, 4, 5f, null, 0b001);

            // Act
            DropChanges(changesSent);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalMultiComponent));
            var multiComp = (MultiComponent)entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data;
            Assert.That(multiComp.StoppedMask, Is.EqualTo(0b101));
        }

        [Test]
        [Description("Verifies that if there is an update to an in-flight stopped field and that update unstops " +
            "the field that the dropped merge of the stopped field correctly clears the stop " +
            "bit so the sent field is not stopped.")]
        public void Test_StoppedMergesCorrectlyWithSentChanges()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // send update with a stopped int value
            UpdateMultiCompEntity(entity, 1, 2f, 3f, 0b001);

            var droppedChanges = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            // update the int value so it is no longer stopped and send it.
            UpdateMultiCompEntity(entity, 4, 5f, null, 0b000);

            var updateChanges = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            // Act
            DropChanges(droppedChanges, updateChanges);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store.ContainsKey(Definition.InternalMultiComponent));
            var multiComp = (MultiComponent)entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data;
            Assert.That(multiComp.StoppedMask, Is.EqualTo(0b000));
        }

        [Test]
        [Description("Verifies that the tests create expected fields masks when adding changes to the change buffer.")]
        public void Test_FieldsMaskBasic()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // Act
            UpdateEntity(entity, 1, 2f, true);
            UpdateMultiCompEntity(entity, 3, 4f, 5f, 0b000);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store[Definition.InternalIntComponent].Data.FieldsMask, Is.EqualTo((uint)0b1));
            Assert.That(entityState.Components.Updates.Store[Definition.InternalFloatComponent].Data.FieldsMask, Is.EqualTo((uint)0b1));
            Assert.That(entityState.Components.Updates.Store[Definition.InternalOrderedComp].Data.FieldsMask, Is.EqualTo((uint)0b0));
            Assert.That(entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data.FieldsMask, Is.EqualTo((uint)0b111));
        }

        [Test]
        [Description("Verifies that two updates with different fields masked merge the masks correctly.")]
        public void Test_FieldsMaskMergeChange()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            // Act
            UpdateMultiCompEntity(entity, 3, null, null, 0b000);
            UpdateMultiCompEntity(entity, null, null, 5f, 0b000);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data.FieldsMask, Is.EqualTo((uint)0b101));
        }

        [Test]
        [Description("Verifies that a dropped change FieldsMask is correctly merged with an existing change.")]
        public void Test_FieldsMaskDroppedMerge()
        {
            // Arrange
            var entity = new Entity(1, 0, false);

            UpdateMultiCompEntity(entity, 3, null, null, 0b000);

            var sentChanges = SendChanges(expectedExistenceChanges: 0, expectedUpdateChanges: 1);

            UpdateMultiCompEntity(entity, null, null, 5f, 0b000);

            DropChanges(sentChanges);

            // Assert
            var metas = changeBuffer.GetEntityMeta();
            var entityState = metas[entity];
            Assert.That(entityState.Components.Updates.Store[Definition.InternalMultiComponent].Data.FieldsMask, Is.EqualTo((uint)0b101));
        }

        private void CreateEntity
        (
            Entity entity,
            int? intComponentValue = null,
            float? floatComponentValue = null,
            bool addOrderedComp = false
        )
        {
            var data = GetComponentData(intComponentValue, floatComponentValue, addOrderedComp);

            var createChange = new EntityCreateChange
            {
                ID = entity,
                Data = ComponentUpdates.New(data),
            };

            changeBuffer.CreateEntity(createChange);
        }

        private void UpdateEntity
        (
            Entity entity,
            int? intComponentValue = null,
            float? floatComponentValue = null,
            bool addOrderedComp = false
        )
        {
            var data = GetComponentData(intComponentValue, floatComponentValue, addOrderedComp);

            var updateChange = new EntityUpdateChange
            {
                ID = entity,
                Data = ComponentUpdates.New(data),
            };

            changeBuffer.UpdateEntity(updateChange);
        }

        private void UpdateMultiCompEntity
        (
            Entity entity,
            int? intField,
            float? float0Field,
            float? float1Field,
            uint stoppedMask
        )
        {
            var data = GetMultiCompData(intField, float0Field, float1Field);
            data.StoppedMask = stoppedMask;

            var comps = new ICoherenceComponentData[] { data };

            var updateChange = new EntityUpdateChange
            {
                ID = entity,
                Data = ComponentUpdates.New(comps),
            };

            changeBuffer.UpdateEntity(updateChange);
        }

        private void DestroyEntity(Entity entity)
        {
            changeBuffer.DestroyEntity(entity, ackedEntities);
        }

        private OutgoingEntityUpdate GetEntityState(
            long priority = 0,
            EntityOperation operation = EntityOperation.Update,
            int? intComponentValue = null,
            float? floatComponentValue = null,
            bool addOrderedComp = false,
            HashSet<uint> destroys = null)
        {
            var data = GetComponentData(intComponentValue, floatComponentValue, addOrderedComp);
            var store = new Dictionary<uint, ComponentChange>();

            foreach (var datum in data)
            {
                switch (datum)
                {
                    case IntComponent intDatum:
                        store.Add(Definition.InternalIntComponent, ComponentChange.New(intDatum));
                        break;
                    case FloatComponent floatDatum:
                        store.Add(Definition.InternalFloatComponent, ComponentChange.New(floatDatum));
                        break;
                    case OrderedComp orderedDatum:
                        store.Add(Definition.InternalOrderedComp, ComponentChange.New(orderedDatum));
                        break;
                }
            }

            return new OutgoingEntityUpdate
            {
                Operation = operation,
                Priority = priority,
                Components = new DeltaComponents
                {
                    Updates = ComponentUpdates.New(store),
                    Destroys = destroys ?? new HashSet<uint>(),
                },
            };
        }

        private ICoherenceComponentData[] GetComponentData
        (
            int? intComponentValue = null,
            float? floatComponentValue = null,
            bool addOrderedComp = false
        )
        {
            var data = new List<ICoherenceComponentData>();

            if (intComponentValue != null)
            {
                data.Add(new IntComponent
                {
                    number = intComponentValue.Value,
                    numberSimulationFrame = simulationFrame,
                    FieldsMask = 0b1
                });
            }

            if (floatComponentValue != null)
            {
                data.Add(new FloatComponent
                {
                    number = floatComponentValue.Value,
                    numberSimulationFrame = simulationFrame,
                    FieldsMask = 0b1
                });
            }

            if (addOrderedComp)
            {
                data.Add(new OrderedComp
                {
                });
            }

            return data.ToArray();
        }

        private ICoherenceComponentData GetMultiCompData
        (
            int? intField,
            float? float0Field,
            float? float1Field
        )
        {
            var mask = 0b000;
            if (intField != null)
            {
                mask |= 0b001;
            }

            if (float0Field != null)
            {
                mask |= 0b010;
            }

            if (float1Field != null)
            {
                mask |= 0b100;
            }

            return new MultiComponent
            {
                intValue = intField ?? 0,
                floatValue0 = float0Field ?? 0f,
                floatValue1 = float1Field ?? 0f,
                intValueSimulationFrame = simulationFrame,
                floatValue0SimulationFrame = simulationFrame,
                floatValue1SimulationFrame = simulationFrame,
                FieldsMask = (uint)mask,
            };
        }

        private Dictionary<Entity, OutgoingEntityUpdate> SendChanges(int expectedExistenceChanges, int expectedUpdateChanges)
        {
            List<EntityChange> existenceChangeBuffer = new List<EntityChange>(32);
            List<EntityChange> updateChangeBuffer = new List<EntityChange>(32);

            var numChanges =
                changeBuffer.GetPrioritizedChanges(existenceChangeBuffer, updateChangeBuffer, ackedEntities);
            Assert.AreEqual(expectedExistenceChanges + expectedUpdateChanges, numChanges,
                "Number of changes is expected existance + expected update changes");
            Assert.AreEqual(expectedExistenceChanges, existenceChangeBuffer.Count,
                "Number of existence changes is correct");
            Assert.AreEqual(expectedUpdateChanges, updateChangeBuffer.Count, "Number of update changes is correct");

            var sentEntities = new List<Entity>();
            foreach (var change in existenceChangeBuffer)
            {
                sentEntities.Add(change.ID);
            }

            foreach (var change in updateChangeBuffer)
            {
                sentEntities.Add(change.ID);
            }

            Dictionary<Entity, OutgoingEntityUpdate> updatesSent = null;
            changeBuffer.AppendSentUpdates(ref updatesSent, sentEntities);

            return updatesSent;
        }

        private void DropChanges(Dictionary<Entity, OutgoingEntityUpdate> changesSent,
            params Dictionary<Entity, OutgoingEntityUpdate>[] unackedSent)
        {
            var unackedSentChanges = new LinkedList<ChangeBuffer>();
            foreach (var unacked in unackedSent)
            {
                unackedSentChanges.AddFirst(new ChangeBuffer(unacked, new Queue<SerializedEntityMessage>(),
                    new Queue<SerializedEntityMessage>(), new SequenceId(0), logger));
            }

            var droppedChanges = new ChangeBuffer(changesSent, new Queue<SerializedEntityMessage>(),
                new Queue<SerializedEntityMessage>(), new SequenceId(0), logger);

            changeBuffer.ResetWithLostChanges(droppedChanges, unackedSentChanges, ackedEntities);
        }

        private void Assert_EntityStateSame(Entity entityID, OutgoingEntityUpdate expectedUpdate)
        {
            Assert.IsTrue(changeBuffer.Buffer.ContainsKey(entityID), "Have Entity in Buffer");

            var bufferedEntityState = changeBuffer.Buffer[entityID];

            Assert.AreEqual(expectedUpdate.Operation, bufferedEntityState.Operation, "Operation");
            Assert.AreEqual(expectedUpdate.IsDestroy, bufferedEntityState.IsDestroy, "Is Destroy");
            Assert.AreEqual(expectedUpdate.Priority, bufferedEntityState.Priority, "Priority");

            Assert.AreEqual(expectedUpdate.Components.Updates.Store.Count,
                bufferedEntityState.Components.Updates.Store.Count, "Same number of component changes");

            foreach (var updateKV in expectedUpdate.Components.Updates.Store)
            {
                var compType = updateKV.Key;
                var expected = updateKV.Value;

                if (!bufferedEntityState.Components.Updates.Store.TryGetValue(compType, out ComponentChange change))
                    Assert.Fail($"Missing component {expected.Data}");

                Assert.IsTrue(expected.Data.Equals(change.Data), "Updated components Are Equal");
            }

            foreach (var compType in expectedUpdate.Components.Destroys)
            {
                Assert.IsTrue(bufferedEntityState.Components.Destroys.Contains(compType),
                    "Component Destroys Are Equal");
            }
        }
    }
}

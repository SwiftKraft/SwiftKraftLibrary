// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Brisk;
    using Brook;
    using Brook.Octet;
    using Common.Tests;
    using Entities;
    using Generated;
    using Log;
    using NUnit.Framework;
    using ProtocolDef;
    using Serializer;
    using SimulationFrame;
    using Coherence.Tests;
    using Coherence.Core.Channels;
    using System.Threading;

    public class OutNetworkChannelTests : CoherenceTest
    {
        private IDefinition root;
        new private TestLogger logger;
        private OutNetworkChannel channel;

        private HashSet<Entity> ackedEntities;
        private Dictionary<Entity, HashSet<uint>> ackedComponentsPerEntity;

        private AbsoluteSimulationFrame simulationFrame;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            root = new Definition();
            logger = new TestLogger();
            channel = new OutNetworkChannel(root, root, null, logger);

            ackedEntities = new HashSet<Entity>();
            ackedComponentsPerEntity = new Dictionary<Entity, HashSet<uint>>();

            simulationFrame = new AbsoluteSimulationFrame
            {
                Frame = 1
            };
        }

        [Test]
        public void EntityCreateAndAck()
        {
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityAComponents = new ICoherenceComponentData[]
            {
            };

            // Create
            channel.CreateEntity(entityA, entityAComponents);

            // Send
            Tick();

            // Inform it was delivered
            HandleAck(true);

            Assert.That(ackedEntities.Count, Is.EqualTo(1));
            Assert.That(ackedEntities.Contains(entityA));
        }

        [Test]
        public void EntityCreateAndDrop()
        {
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityAComponents = new ICoherenceComponentData[]
            {
            };

            // Create
            channel.CreateEntity(entityA, entityAComponents);

            // Send
            Tick();

            // Inform it was dropped
            HandleAck(false);

            Assert.That(ackedEntities.Count, Is.EqualTo(0));
            Assert.That(!ackedEntities.Contains(entityA));
        }

        [Test]
        public void EntityCreateDropResend()
        {
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityAComponents = new ICoherenceComponentData[]
            {
            };

            // Create
            channel.CreateEntity(entityA, entityAComponents);

            // Send
            Tick();

            // Inform it was dropped
            HandleAck(false);

            Assert.That(ackedEntities.Count, Is.EqualTo(0));
            Assert.That(!ackedEntities.Contains(entityA));

            // Resend
            var packet = Tick();

            var updates = GetEntityUpdates(packet);

            Assert.That(updates.Count, Is.EqualTo(1));
            var meta = updates[0].Meta;
            Assert.That(meta.EntityId == entityA);
            Assert.That(meta.IsDestroyed == false);
        }

        [Test]
        public void EntityCreateUpdateBothDropResend()
        {
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityAComponents = new ICoherenceComponentData[]
            {
            };

            // Create
            channel.CreateEntity(entityA, entityAComponents);

            Tick();

            // Update
            entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 42,
                    numberSimulationFrame = simulationFrame,
                    FieldsMask = 0b1
                },
            };
            channel.UpdateComponents(entityA, entityAComponents);

            Tick();

            // Inform they were dropped
            HandleAck(false);
            HandleAck(false);

            Assert.That(ackedEntities.Count, Is.EqualTo(0));
            Assert.That(!ackedEntities.Contains(entityA));

            // Resend
            Tick();

            // Inform it was delivered
            HandleAck(true);

            Assert.That(ackedEntities.Count, Is.EqualTo(1),
                "Entity should be ack or the update cleared the create state.");
            Assert.That(ackedEntities.Contains(entityA),
                "Entity should be ack or the update cleared the create state.");
        }

        [Test]
        public void EntityDropCreateGetUpdateUpdateAgainResendCreate()
        {
            // This test where an entity has a component with an int value that is increasing.
            // It is initialized to 1 when created and increamented with each update.
            // The test is correct when the latest send of the entity sends the latest value of
            // the component, so when the resend happens after droppped create is acked, the value
            // of the component should be 3.

            // Create with value of 1
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 1,
                    numberSimulationFrame = simulationFrame
                },
            };
            channel.CreateEntity(entityA, entityAComponents);

            // Send 1
            Tick();

            // Update value to 2
            simulationFrame = new AbsoluteSimulationFrame
            {
                Frame = 2
            };
            entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 2,
                    numberSimulationFrame = simulationFrame,
                    FieldsMask = 0b1
                },
            };
            channel.UpdateComponents(entityA, entityAComponents);

            // Send 2
            Tick();

            // Update to 3
            simulationFrame = new AbsoluteSimulationFrame
            {
                Frame = 3
            };
            entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 3,
                    numberSimulationFrame = simulationFrame,
                    FieldsMask = 0b1
                },
            };
            channel.UpdateComponents(entityA, entityAComponents);

            // Inform the create 1 was dropped, but the update 2 was delivered
            HandleAck(false);
            HandleAck(true);

            // since the create was not acked, there should be no acked entities.
            Assert.That(ackedEntities.Count, Is.EqualTo(0));
            Assert.That(!ackedEntities.Contains(entityA));

            // Resend create
            var packet = Tick();

            var updates = GetEntityUpdates(packet);
            Assert.That(updates.Count, Is.EqualTo(1));
            var meta = updates[0].Meta;
            Assert.That(meta.EntityId == entityA);
            Assert.That(meta.IsDestroyed == false);

            // The resent create should be the aggregate of all changes so far.
            var receiveData = (IntComponent)updates[0].Components.Updates.Store[Definition.InternalIntComponent].Data;
            Assert.That(receiveData.number, Is.EqualTo(3));
        }

        [Test]
        public void EntityDropUpdateResendInOrder()
        {
            // Similar to the dropped create test, this test is also where an entity
            // has a component with an int value that is increasing.
            // It is initialized to 1 when created and increamented with each update.
            // The test is correct when the latest send of the entity sends the latest value of
            // the component, so when the resend happens after droppped update is acked, the value
            // of the component should be 4.

            // Create with value of 1
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 1,
                    numberSimulationFrame = simulationFrame
                },
            };
            channel.CreateEntity(entityA, entityAComponents);

            // Send 1
            Tick();

            // Update value to 2
            entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 2,
                    numberSimulationFrame = simulationFrame,
                    FieldsMask = 0b1
                },
            };
            channel.UpdateComponents(entityA, entityAComponents);

            // Send 2
            Tick();

            // Update to 3
            entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 3,
                    numberSimulationFrame = simulationFrame,
                    FieldsMask = 0b1
                },
            };
            channel.UpdateComponents(entityA, entityAComponents);

            // Send 3
            Tick();

            // Update to 4
            entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 4,
                    numberSimulationFrame = simulationFrame,
                    FieldsMask = 0b1
                },
            };
            channel.UpdateComponents(entityA, entityAComponents);

            // Create with 1 was delivered
            HandleAck(true);

            // Update with 2 was dropped
            HandleAck(false);

            // Update with 3 was delivered
            HandleAck(true);

            Assert.That(ackedEntities.Count, Is.EqualTo(1));
            Assert.That(ackedEntities.Contains(entityA));

            // Send 4 - since there was an update the value sent should be 4
            var packet = Tick();

            var updates = GetEntityUpdates(packet);
            Assert.That(updates.Count, Is.EqualTo(1));
            var meta = updates[0].Meta;
            Assert.That(meta.EntityId == entityA);
            Assert.That(meta.IsDestroyed == false);

            // The resent create should be the aggregate of all changes so far.
            var receiveData = (IntComponent)updates[0].Components.Updates.Store[Definition.InternalIntComponent].Data;
            Assert.That(receiveData.number, Is.EqualTo(4));
        }

        [Test]
        public void DroppedChanges_ShouldHaveTopPriority()
        {
            // Arrange
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 1,
                    numberSimulationFrame = simulationFrame
                },
            };

            var entityB = new Entity(2, 0, Entity.Relative);
            var entityBComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 1,
                    numberSimulationFrame = simulationFrame
                },
            };

            // Act
            // Create EntityA and EntityB and send
            channel.CreateEntity(entityA, entityAComponents);
            channel.CreateEntity(entityB, entityBComponents);
            Tick();

            // Update EntityB and send, this will be dropped!
            channel.UpdateComponents(entityB, entityBComponents);
            Tick();

            // Update EntityA and hold back to increase priority
            channel.UpdateComponents(entityA, entityAComponents);

            // Call MarkAsSent multiple times to increase priority of held back changes (Update EntityA)
            // LastSerializationResult must be cleared to prevent duplicate changeBuffer manipulations
            channel.ClearLastSerializationResult();
            _ = channel.MarkAsSent(new SequenceId(0));
            _ = channel.MarkAsSent(new SequenceId(0));
            _ = channel.MarkAsSent(new SequenceId(0));
            _ = channel.MarkAsSent(new SequenceId(0));

            // Notify Entity create was received but update was dropped!
            HandleAck(true);
            HandleAck(false);

            // Create and queue packet
            var packet = Tick();

            // Assert
            var updates = GetEntityUpdates(packet);
            Assert.AreEqual(entityB, updates.First().Meta.EntityId,
                "EntityB update should have bigger priority because it was dropped");
        }

        [TestCase(true)]
        [TestCase(false)]
        [Description("Validates that a component remove before sending a create or update" +
                     "correctly clears the component from the changebuffer.")]
        public void TestRemoveBeforeSendClearsComponent(bool isCreate)
        {
            // Arrange
            // Add either a create or an update to the out connection
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 1,
                    numberSimulationFrame = simulationFrame
                },
                new FloatComponent
                {
                    number = 0.5f,
                    numberSimulationFrame = simulationFrame
                },
            };

            if (isCreate)
            {
                channel.CreateEntity(entityA, entityAComponents);
            }
            else
            {
                channel.UpdateComponents(entityA, entityAComponents);
            }

            // Act
            // Remove the component and send
            channel.RemoveComponents(entityA, new[]
            {
                Definition.InternalIntComponent
            }, ackedComponentsPerEntity);
            var packet = Tick();

            // Assert
            // The change buffer should have no information about the component
            var updates = GetEntityUpdates(packet);
            Assert.That(updates.Count, Is.EqualTo(1));
            var meta = updates[0].Meta;
            Assert.That(meta.EntityId == entityA);
            Assert.That(updates[0].Components.Updates.Store.Keys, Has.No.Member(Definition.InternalIntComponent));
            Assert.That(updates[0].Components.Updates.Store.ContainsKey(Definition.InternalFloatComponent), Is.True);
            Assert.That(updates[0].Components.Destroys, Is.Empty);
        }

        [Test]
        [Description("Validates that a component remove before sending a component update" +
                     "correctly clears the component from the changebuffer. This means the entity create" +
                     "was ackd but the entity component update has not yet been.")]
        public void TestRemoveBeforeSendClearsComponentAfterEntityAck()
        {
            // Arrange
            // Create and ack the entity
            var entityA = new Entity(1, 0, Entity.Relative);

            channel.CreateEntity(entityA, new ICoherenceComponentData[] { });

            Tick();
            HandleAck(true);

            // Add an update for the component.
            var entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 1,
                    numberSimulationFrame = simulationFrame
                },
            };
            channel.UpdateComponents(entityA, entityAComponents);

            // Act
            // Remove the component and send
            channel.RemoveComponents(entityA, new[]
            {
                Definition.InternalIntComponent
            }, ackedComponentsPerEntity);
            var packet = Tick();

            // Assert
            // The change buffer should have no information about the component
            var updates = GetEntityUpdates(packet);
            Assert.That(updates.Count, Is.EqualTo(1));
            var meta = updates[0].Meta;
            Assert.That(meta.EntityId == entityA);
            Assert.That(updates[0].Components.Updates.Store, Has.No.Member(Definition.InternalIntComponent));
            Assert.That(updates[0].Components.Destroys, Is.Empty);
        }

        [Test]
        [Description("Validates that a component remove before sending a component update" +
                     " correctly clears the component from the changebuffer even if another component" +
                     " was ackd.")]
        public void TestRemoveBeforeSendDoesNotClearComponentIfOtherComponentAcked()
        {
            // Arrange
            // Create and ack the entity
            var entityA = new Entity(1, 0, Entity.Relative);

            channel.CreateEntity(entityA, new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 1,
                    numberSimulationFrame = simulationFrame
                }
            });

            Tick();
            HandleAck(true);

            // Add an update for the component.
            var entityAComponents = new ICoherenceComponentData[]
            {
                new FloatComponent
                {
                    number = 0.5f,
                    numberSimulationFrame = simulationFrame
                },
            };
            channel.UpdateComponents(entityA, entityAComponents);

            // Act
            // Remove the component and send
            channel.RemoveComponents(entityA, new[]
            {
                Definition.InternalIntComponent,
                Definition.InternalFloatComponent
            }, ackedComponentsPerEntity);
            var packet = Tick();

            // Assert
            // The change buffer should have no information about the component
            var updates = GetEntityUpdates(packet);
            Assert.That(updates.Count, Is.EqualTo(1));
            var meta = updates[0].Meta;
            Assert.That(meta.EntityId == entityA);
            Assert.That(updates[0].Components.Updates.Store, Has.No.Member(Definition.InternalFloatComponent));
            Assert.That(updates[0].Components.Destroys, Has.No.Member(Definition.InternalFloatComponent));
            Assert.That(updates[0].Components.Destroys, Does.Contain(Definition.InternalIntComponent));
        }

        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        [TestCase(false, true)]
        [Description("Validates that a component remove after sending a create or update" +
                     "correctly posts a remove change and does not clear the component. Validates the same" +
                     "whether or not the sent component was acked.")]
        public void TestRemoveAfterSendAppliesComponentRemove(bool isCreate, bool sendAcked)
        {
            // Arrange
            // Add either a create or an update to the out connection
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 1,
                    numberSimulationFrame = simulationFrame
                },
            };

            if (isCreate)
            {
                channel.CreateEntity(entityA, entityAComponents);
            }
            else
            {
                channel.UpdateComponents(entityA, entityAComponents);
            }

            // Send the create / udpate so there's a change in flight
            Tick();

            if (sendAcked)
            {
                // Acking the change, removes the in-flight change but
                // updates the internal component acked state so we can
                // know if a component is known but the other side.
                HandleAck(true);
            }

            // Act
            // Remove the component and send
            channel.RemoveComponents(entityA, new[]
            {
                Definition.InternalIntComponent
            }, ackedComponentsPerEntity);
            var packet = Tick();

            // Assert
            // The change buffer should have no information about the component
            var updates = GetEntityUpdates(packet);
            Assert.That(updates.Count, Is.EqualTo(1));
            var meta = updates[0].Meta;
            Assert.That(meta.EntityId == entityA);
            Assert.That(updates[0].Components.Updates.Store, Has.No.Member(Definition.InternalIntComponent));
            Assert.That(updates[0].Components.Destroys, Does.Contain(Definition.InternalIntComponent));
        }

        [TestCase(true, TestName = "SendUpdate")]
        [TestCase(false, TestName = "SendRemove")]
        [Description("Verifies that HasChangesForEntity returns false only when the sent " +
            "changes are acked for that entity.")]
        public void Test_SentAndAckedChangesClearHasChangesForEntity(bool sendUpdate)
        {
            // Arrange
            var entityA = new Entity(1, 0, Entity.Relative);
            var intComp = new IntComponent
            {
                number = 1,
                numberSimulationFrame = simulationFrame
            };
            var entityAComponents = new ICoherenceComponentData[]
            {
                intComp,
            };

            if (sendUpdate)
            {
                // send an entity update
                channel.UpdateComponents(entityA, entityAComponents);
            }
            else
            {
                // send an entity update and ack it so the out component knows about the comp.
                channel.UpdateComponents(entityA, entityAComponents);
                Tick();
                HandleAck(true);

                // now send the comp remove.
                channel.RemoveComponents(entityA, new uint[] { intComp.GetComponentType() }, ackedComponentsPerEntity);
            }

            Assert.That(channel.HasChangesForEntity(entityA), Is.True);

            // Act & Assert 1 - send changes but don't ack
            Tick();
            Assert.That(channel.HasChangesForEntity(entityA), Is.True);

            // Act & Assert 2 - dropped changes resend.
            HandleAck(false);
            Tick();
            Assert.That(channel.HasChangesForEntity(entityA), Is.True);

            // Act & Assert 3 - send changes again
            Tick();
            Assert.That(channel.HasChangesForEntity(entityA), Is.True);

            // Act & Assert 4 - ack changes
            HandleAck(true);
            Tick();
            Assert.That(channel.HasChangesForEntity(entityA), Is.False);
        }

        [Test]
        [Description("Tests that ordered component update is not resent on every tick if there are no other pending changes to be sent.")]
        public void OrderedComponentUpdate_WhenNoChanges_ShouldNotBeResent()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);

            // Act - Add ordered component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new OrderedComp() });
            var packet = Tick();

            // Assert - check that sent change includes Ordered component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalOrderedComp },
                removedComponents: new uint[] { });

            // Act - send again
            packet = Tick();

            // Assert - check that nothing was sent
            Assert.That(GetEntityUpdates(packet).Count, Is.EqualTo(0));
        }

        [Test]
        [Description("Tests that an ordered component update which is in-flight is resent with any other component update.")]
        public void OrderedComponentUpdate_WhenSendingAnyComponentUpdate_ShouldBeResent()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);

            // Add ordered component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new OrderedComp() });
            Tick();

            // Act - add some other component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new IntComponent() });
            var packet = Tick();

            // Assert - check that sent change includes both Ordered and Int component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalOrderedComp, Definition.InternalIntComponent },
                removedComponents: new uint[] { });
        }

        [Test]
        [Description("Tests that an ordered component update which is in-flight is resent with any other component destroy.")]
        public void OrderedComponentUpdate_WhenSendingAnyComponentDestroy_ShouldBeResent()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);

            // Add ordered component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new OrderedComp() });
            Tick();

            // Act - add some other component destroy and send it
            channel.RemoveComponents(entity, new uint[] { Definition.InternalIntComponent }, ackedComponentsPerEntity);
            var packet = Tick();

            // Assert - check that sent change includes both Ordered and Int component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalOrderedComp },
                removedComponents: new uint[] { Definition.InternalIntComponent });
        }

        [Test]
        [Description("Tests that ordered component remove is not resent on every tick if there are no other pending changes to be sent.")]
        public void OrderedComponentRemove_WhenNoChanges_ShouldNotBeResent()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);
            channel.CreateEntity(entity, new ICoherenceComponentData[] { new OrderedComp() });
            Tick();
            HandleAck(true);

            // Act - Add ordered component update and send it
            channel.RemoveComponents(entity, new uint[] { Definition.InternalOrderedComp }, ackedComponentsPerEntity);
            var packet = Tick();

            // Assert - check that sent change includes Ordered component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { },
                removedComponents: new uint[] { Definition.InternalOrderedComp });

            // Act - send again
            packet = Tick();

            // Assert - check that nothing was sent
            Assert.That(GetEntityUpdates(packet).Count, Is.EqualTo(0));
        }

        [Test]
        [Description("Tests that an ordered component remove which is in-flight is resent with any other component update.")]
        public void OrderedComponentRemove_WhenSendingAnyComponentUpdate_ShouldBeResent()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);
            channel.CreateEntity(entity, new ICoherenceComponentData[] { new OrderedComp() });
            Tick();
            HandleAck(true);

            // Add ordered component destroy and send it
            channel.RemoveComponents(entity, new uint[] { Definition.InternalOrderedComp }, ackedComponentsPerEntity);
            Tick();

            // Act - add some other component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new IntComponent() });
            var packet = Tick();

            // Assert - check that sent change includes both Ordered and Int component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalIntComponent },
                removedComponents: new uint[] { Definition.InternalOrderedComp });
        }

        [Test]
        [Description("Tests that an ordered component remove which is in-flight is resent with any other component destroy.")]
        public void OrderedComponentRemove_WhenSendingAnyComponentDestroy_ShouldBeResent()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);
            channel.CreateEntity(entity, new ICoherenceComponentData[] { new OrderedComp() });
            Tick();
            HandleAck(true);

            // Add ordered component destroy and send it
            channel.RemoveComponents(entity, new uint[] { Definition.InternalOrderedComp }, ackedComponentsPerEntity);
            Tick();

            // Act - add some other component destroy and send it
            channel.RemoveComponents(entity, new uint[] { Definition.InternalIntComponent }, ackedComponentsPerEntity);
            var packet = Tick();

            // Assert - check that sent change includes both Ordered and Int component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { },
                removedComponents: new uint[] { Definition.InternalIntComponent, Definition.InternalOrderedComp });
        }

        [Test]
        [Description("Tests if an ordered component was sent together with some other component, that both components are resent.")]
        public void OrderedComponent_WhenSentWithAnotherComponent_ShouldResendBoth()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);

            // Add ordered component destroy and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new OrderedComp(), new FloatComponent() });
            Tick();

            // Act - add some other component destroy and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new IntComponent() });
            var packet = Tick();

            // Assert - check that sent change includes all components
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalIntComponent, Definition.InternalFloatComponent, Definition.InternalOrderedComp },
                removedComponents: new uint[] { });
        }

        [Test]
        [Description("Tests that after an ordered component was acked that it's no longer resent.")]
        public void OrderedComponent_WhenAcked_ShouldNotResend()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);

            // Add ordered component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new OrderedComp() });
            Tick();

            // Act - add some other component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new IntComponent() });
            var packet = Tick();

            // Assert - check that sent change includes both Ordered and Int component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalIntComponent, Definition.InternalOrderedComp },
                removedComponents: new uint[] { });

            // Act - ack ordered component and send another component update
            HandleAck(true);
            HandleAck(true);
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new IntComponent() });
            packet = Tick();

            // Assert - check that sent change does not include the ordered component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalIntComponent },
                removedComponents: new uint[] { });
        }

        [Test]
        [Description("Tests that after an ordered component was acked that it's no longer resent " +
            "even if it was resent once and that is still in-flight.")]
        public void OrderedComponent_WhenAckedOnce_ShouldNotResend()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);

            // Add ordered component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new OrderedComp() });
            Tick();

            // Act - add some other component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new IntComponent() });
            var packet = Tick();

            // Assert - check that sent change includes both Ordered and Int component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalIntComponent, Definition.InternalOrderedComp },
                removedComponents: new uint[] { });

            // Act - ack ordered component and send another component update
            HandleAck(true);
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new FloatComponent() });
            packet = Tick();

            // Assert - check that sent change does not include the ordered component
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalFloatComponent },
                removedComponents: new uint[] { });
        }

        [Test]
        [Description("Tests that if there are two ordered component updates in-flight (in separate updates) that they are both resent.")]
        public void OrderedComponent_WhenSentTwoOrderedComponents_ShouldResendBoth()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);

            // Add ordered component destroy and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new OrderedComp() });
            Tick();

            // Act - add another ordered component and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new Ordered2Comp() });
            var packet = Tick();

            // Assert - check that sent change includes both ordered component updates
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalOrderedComp, Definition.InternalOrdered2Comp },
                removedComponents: new uint[] { });

            // Act - ack ordered1 component and send another component update
            HandleAck(true);
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new IntComponent() });
            packet = Tick();

            // Assert - check that sent change includes both ordered components
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalIntComponent, Definition.InternalOrderedComp, Definition.InternalOrdered2Comp },
                removedComponents: new uint[] { });

            // Act - ack all
            HandleAck(true);
            HandleAck(true);
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new FloatComponent() });
            packet = Tick();

            // Assert - check that sent change includes both ordered components
            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalFloatComponent },
                removedComponents: new uint[] { });
        }

        [Test]
        [Description("Tests that if there are two changes for an ordered component in-flight and the first is acked, the second change is still resent until acked. " +
            "This tests the parent -> unparent -> reparent scenario.")]
        public void OrderedComponent_WhenAckedFirst_KeepsResendSecond()
        {
            // Arrange
            var entity = new Entity(1, 0, Entity.Relative);

            // Add ordered component update and send it
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new OrderedComp() });
            var packet = Tick();

            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalOrderedComp },
                removedComponents: new uint[] { });

            // Act - add ordered component destroy and send it
            channel.RemoveComponents(entity, new uint[] { Definition.InternalOrderedComp }, ackedComponentsPerEntity);
            packet = Tick();

            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { },
                removedComponents: new uint[] { Definition.InternalOrderedComp });

            // Act - add some other component update to see if ordered is correctly resent
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new IntComponent() });
            packet = Tick();

            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalIntComponent },
                removedComponents: new uint[] { Definition.InternalOrderedComp });

            // Act - ack first ordered component change (update)
            HandleAck(true);

            // Add some other component update to see if ordered is correctly resent
            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new FloatComponent() });
            packet = Tick();

            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalIntComponent, Definition.InternalFloatComponent },
                removedComponents: new uint[] { Definition.InternalOrderedComp });

            // Act - ack second ordered component change (destroy)
            HandleAck(true);
            HandleAck(true);
            HandleAck(true);

            channel.UpdateComponents(entity, new ICoherenceComponentData[] { new IntComponent() });
            packet = Tick();

            Assert_SentEntityUpdate(packet, entity,
                updatedComponents: new uint[] { Definition.InternalIntComponent },
                removedComponents: new uint[] { });
        }

        [TestCase(true)]
        [TestCase(false)]
        [Description("Tests that MarkAsSent and OnDeliveryInfo don't throw if there was nothing even serialized.")]
        public void MarkAsSent_WhenNotSerialzed_DoesntThrow(bool wasDelivered)
        {
            var sequenceID = new SequenceId(1);

            channel.MarkAsSent(sequenceID);

            channel.OnDeliveryInfo(new DeliveryInfo()
            {
                PacketSequenceId = sequenceID,
                WasDelivered = wasDelivered
            }, ref ackedEntities, ref ackedComponentsPerEntity);
        }

        private OutOctetStream Tick(bool holdOnToCommands = false)
        {
            var octetStream = new OutOctetStream(Brisk.DefaultMTU);
            var stream = new OutBitStream(octetStream);
            var ctx = new SerializerContext<IOutBitStream>(stream, false, logger);

            channel.Serialize(ctx, simulationFrame, holdOnToCommands, ackedEntities);

            stream.Flush();

            _ = channel.MarkAsSent(new SequenceId(0));

            // Need to tick a little time so OrderedUpdateTime increases
            Thread.Sleep(16);

            return octetStream;
        }

        private void HandleAck(bool delivered)
        {
            channel.OnDeliveryInfo(new DeliveryInfo
            {
                WasDelivered = delivered,
                PacketSequenceId = new SequenceId(0),
            }, ref ackedEntities, ref ackedComponentsPerEntity);
        }

        private List<IncomingEntityUpdate> GetEntityUpdates(OutOctetStream stream)
        {
            var octetStream = new InOctetStream(stream.Close().ToArray());
            var bitStream = new InBitStream(octetStream, octetStream.RemainingOctetCount * 8);

            Assert.That(octetStream != null);

            if (DeserializeCommands.DeserializeCommand(bitStream, out MessageType messageType))
            {
                Assert.That(messageType == MessageType.EcsWorldUpdate);
                var updates = new List<IncomingEntityUpdate>();
                Deserialize.ReadWorldUpdate(updates, simulationFrame, Vector3.Zero, root, bitStream, root, logger);
                return updates;
            }

            return new List<IncomingEntityUpdate>();
        }

        private void Assert_SentEntityUpdate(OutOctetStream stream, Entity id, uint[] updatedComponents, uint[] removedComponents)
        {
            var updates = GetEntityUpdates(stream);
            Assert.That(updates.Count, Is.EqualTo(1));

            var sentUpdate = updates[0];

            Assert.That(sentUpdate.Entity, Is.EqualTo(id));

            Assert.That(sentUpdate.Components.Updates.Store.Keys, Is.EquivalentTo(updatedComponents));
            Assert.That(sentUpdate.Components.Destroys, Is.EquivalentTo(removedComponents));
        }
    }
}

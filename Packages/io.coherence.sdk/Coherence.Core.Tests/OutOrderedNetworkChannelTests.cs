// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using System.Collections.Generic;
    using Brisk;
    using Brook;
    using Brook.Octet;
    using Channels;
    using Coherence.Tests;
    using Connection;
    using Entities;
    using Generated;
    using NUnit.Framework;
    using ProtocolDef;
    using Serializer;
    using SimulationFrame;

    public class OutOrderedNetworkChannelTests : CoherenceTest
    {
        private IDefinition definition;
        private OutOrderedNetworkChannel channel;
        private HashSet<Entity> ackedEntities;
        private Dictionary<Entity, HashSet<uint>> ackedComponentsPerEntity;
        private readonly AbsoluteSimulationFrame simulationFrame = new()
        {
            Frame = 1
        };

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            definition = new Definition();
            channel = new OutOrderedNetworkChannel(definition, definition, null, logger);
            ackedEntities = new HashSet<Entity>();
            ackedComponentsPerEntity = new Dictionary<Entity, HashSet<uint>>();
        }

        [Test]
        public void ShouldNotSend_WithoutQueuedCommands()
        {
            var (result, _) = SerializeTick();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ShouldNotSend_UnackedMessagesTwice()
        {
            var entityID = new Entity(1, 0, false);
            channel.PushCommand(CreateTestCommand(), MessageTarget.AuthorityOnly, entityID, false);
            channel.PushCommand(CreateTestCommand(), MessageTarget.AuthorityOnly, entityID, false);

            var (res1, _) = SerializeTick();
            Assert.That(res1.MessagesSent.Count, Is.EqualTo(2));

            var (res2, _) = SerializeTick();
            Assert.That(res2, Is.Null);
        }

        [Test]
        public void ShouldResend_OnFailedDelivery()
        {
            var entityID = new Entity(1, 0, false);
            channel.PushCommand(CreateTestCommand(), MessageTarget.AuthorityOnly, entityID, false);
            channel.PushCommand(CreateTestCommand(), MessageTarget.AuthorityOnly, entityID, false);

            var (res1, _) = SerializeTick();
            Assert.That(res1.MessagesSent.Count, Is.EqualTo(2));

            HandleAck(false);

            var (res2, _) = SerializeTick();
            Assert.That(res2.MessagesSent.Count, Is.EqualTo(2));
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

        private (OrderedChannelSerializationResult, OutOctetStream) SerializeTick()
        {
            var octetStream = new OutOctetStream(Brisk.DefaultMTU);
            var stream = new OutBitStream(octetStream);
            var ctx = new SerializerContext<IOutBitStream>(stream, false, logger);

            channel.Serialize(ctx, simulationFrame, false, ackedEntities);

            stream.Flush();

            _ = channel.MarkAsSent(new SequenceId(0));

            return (channel.LastSerializationResult as OrderedChannelSerializationResult, octetStream);
        }

        private void HandleAck(bool delivered) =>
            channel.OnDeliveryInfo(new DeliveryInfo
            {
                WasDelivered = delivered,
                PacketSequenceId = new SequenceId(0),
            }, ref ackedEntities, ref ackedComponentsPerEntity);

        private IEntityCommand CreateTestCommand()
        {
            var entityID = new Entity(1, 0, false);
            return definition.CreateAuthorityRequest(
                entityID,
                new ClientID(1),
                AuthorityType.State
            );
        }
    }
}

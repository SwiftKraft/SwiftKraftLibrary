// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using Brisk;
    using Brook;
    using Brook.Octet;
    using Common.Tests;
    using Connection;
    using Entities;
    using Generated;
    using Log;
    using NUnit.Framework;
    using Serializer;
    using SimulationFrame;
    using Coherence.Tests;
    using Moq;
    using Coherence.Core.Channels;
    using Coherence.Common;

    public class OutConnectionTests : CoherenceTest
    {
        private FakeConnection fakeConnection;
        private Mock<IOutNetworkChannel> channelMock;
        private HashSet<Entity> ackedEntities;
        private OutConnection outConnection;
        private AbsoluteSimulationFrame simulationFrame;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            fakeConnection = new FakeConnection(logger);

            channelMock = CreateChannel();
            ackedEntities = new HashSet<Entity>();
            outConnection = new OutConnection(
                fakeConnection,
                new Dictionary<ChannelID, IOutNetworkChannel>() { { ChannelID.Default, channelMock.Object } },
                ackedEntities,
                logger
            );

            simulationFrame = new AbsoluteSimulationFrame
            {
                Frame = 1
            };
        }

        [Test]
        [Description("Verifies that when no channel has any changes, a packet shouldn't be sent.")]
        public void Update_WhenNoChannelHasChanges_ShouldNotSendAPacket()
        {
            outConnection.Update(simulationFrame);

            Assert.That(fakeConnection.SentPackets, Is.Empty);
        }

        [Test]
        [Description("Verifies that when channel has some changes, a packet should be sent with correct headers.")]
        public void Update_WhenChannelHasChange_ShouldSendAPacket()
        {
            var floatingOrigin = new Vector3d(12, 11, 100);
            outConnection.SetFloatingOrigin(floatingOrigin);

            simulationFrame = new AbsoluteSimulationFrame() { Frame = 420 };

            channelMock.Setup(c => c.HasChanges(It.IsAny<IReadOnlyCollection<Entity>>())).Returns(true);
            channelMock.Setup(c => c.Serialize(It.IsAny<SerializerContext<IOutBitStream>>(), simulationFrame, false, ackedEntities)).Returns(true);

            outConnection.Update(simulationFrame);

            Assert.That(fakeConnection.SentPackets.Count, Is.EqualTo(1));

            Assert_PacketHeaders(GetSentPackets()[0], simulationFrame, floatingOrigin);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(14)]
        [Description("Verifies that when a channel has some changes, a packet should be sent with matching channelID.")]
        [Ignore("Enable this test once channels are fully implemented on RSL")]
        public void Update_WhenChannelHasChange_ShouldSerializeCorrectChannelID(ChannelID channelID)
        {
            if (channelID == ChannelID.Default)
            {
                // Use the default channel
                channelMock.Setup(c => c.HasChanges(It.IsAny<IReadOnlyCollection<Entity>>())).Returns(true);
            }
            else
            {
                // Create a new channel
                var newChannelMock = AddChannel(channelID);
                newChannelMock.Setup(c => c.HasChanges(It.IsAny<IReadOnlyCollection<Entity>>())).Returns(true);
            }

            outConnection.Update(simulationFrame);

            Assert.That(fakeConnection.SentPackets.Count, Is.EqualTo(1));

            Assert_PacketChannelID(GetSentPackets()[0], channelID);
        }

        [Test]
        [Description("Validates that calling HoldChangesForEntity results in logged errors if updates " +
            "to components are received.")]
        public void Test_HoldCommandsForEntityThrowsErrorsWhenEntityUpdated()
        {
            // Arrange
            var entityA = new Entity(1, 0, Entity.Relative);
            outConnection.HoldChangesForEntity(entityA);

            var entityAComponents = new ICoherenceComponentData[]
            {
                new IntComponent
                {
                    number = 1,
                    numberSimulationFrame = simulationFrame
                },
            };

            // Act
            outConnection.UpdateEntity(entityA, entityAComponents);

            // Assert
            Assert.That(logger.GetCountForErrorID(Error.CoreOutConnectionUpdateOnHeldEntity), Is.EqualTo(1));
        }

        [Test]
        [Description("Validates that calling HoldChangesForEntity results in logged errors if components " +
            "are removed.")]
        public void Test_HoldCommandsForEntityThrowsErrorsWhenEntityComponentRemoved()
        {
            // Arrange
            var entityA = new Entity(1, 0, Entity.Relative);
            outConnection.HoldChangesForEntity(entityA);

            // Act
            outConnection.RemoveComponent(entityA, new uint[] { 42 });

            // Assert
            Assert.That(logger.GetCountForErrorID(Error.CoreOutConnectionRemoveComponentHeldEntity), Is.EqualTo(1));
        }

        [Test]
        [Description("Verifies that CanSendUpdates depends on the state of held changes for an entity.")]
        public void Test_CanSendUpdatesDependsOnHoldCommandsForEntity()
        {
            // Arrange
            var entityA = new Entity(1, 0, Entity.Relative);
            var entityB = new Entity(2, 0, Entity.Relative);

            // Act & Assert 1
            outConnection.HoldChangesForEntity(entityA);
            Assert.That(outConnection.CanSendUpdates(entityA), Is.False);
            Assert.That(outConnection.CanSendUpdates(entityB), Is.True);

            channelMock.Setup(c => c.HasChangesForEntity(entityA)).Returns(false);

            // Act & Assert 2
            Tick(); // clears out the held up entity since there are no changes remaining.
            Assert.That(outConnection.CanSendUpdates(entityA), Is.True);
            Assert.That(outConnection.CanSendUpdates(entityB), Is.True);
        }

        [Test]
        [Description("Verifies that the held changes for an entity are cleared only when the sent " +
            "changes are acked for that entity.")]
        public void Test_SentAndAckedChangesClearHeldEntities()
        {
            // Arrange
            var entityA = new Entity(1, 0, Entity.Relative);

            // setup the outconnection OnAuthorityTransferred callback to count calls
            var transferCalls = 0;
            outConnection.OnAuthorityTransferred += (id) =>
            {
                if (id == entityA)
                {
                    transferCalls++;
                }
            };

            channelMock.Setup(c => c.HasChangesForEntity(entityA)).Returns(true);

            outConnection.HoldChangesForEntity(entityA);
            Assert.That(outConnection.CanSendUpdates(entityA), Is.False);

            // Verify that holdOnToCommands is true because there is an entity being held
            channelMock.Setup(c => c.Serialize(It.IsAny<SerializerContext<IOutBitStream>>(), simulationFrame, true, ackedEntities)).Returns(true).Verifiable();
            channelMock.Setup(c => c.HasChanges(It.IsAny<IReadOnlyCollection<Entity>>())).Returns(true);

            Tick();
            Assert.That(outConnection.CanSendUpdates(entityA), Is.False);
            channelMock.Verify();

            channelMock.Setup(c => c.HasChanges(It.IsAny<IReadOnlyCollection<Entity>>())).Returns(false);
            channelMock.Setup(c => c.HasChangesForEntity(entityA)).Returns(false);

            Tick();
            Assert.That(outConnection.CanSendUpdates(entityA), Is.True);


            // Tick again and assert that the transfer was completed only once.
            Tick();
            Assert.That(transferCalls, Is.EqualTo(1));
        }

        [Test]
        [Description("Verifies that channels must be non-null with valid ChannelIDs")]
        public void Test_AddChannel_MustBeValid()
        {
            // Channel cannot be null
            Assert.Throws<ArgumentNullException>(() => outConnection.AddChannel((ChannelID)1, null));

            // ChannelID must be < 15 (EndOfChannels)
            Assert.Throws<ArgumentException>(() => AddChannel(ChannelID.EndOfChannels));
            Assert.Throws<ArgumentException>(() => AddChannel((ChannelID)100));

            // We can add channels as long as their ChannelIDs are unique
            Assert.DoesNotThrow(() => AddChannel((ChannelID)1));
            Assert.DoesNotThrow(() => AddChannel((ChannelID)5));
            Assert.DoesNotThrow(() => AddChannel((ChannelID)8));
            Assert.DoesNotThrow(() => AddChannel((ChannelID)2));

            // If a ChannelID is already used, an exception is thrown
            Assert.Throws<Exception>(() => AddChannel((ChannelID)1));

            // ChannelID 0 is already used by the default channel
            Assert.Throws<Exception>(() => AddChannel(ChannelID.Default));
        }

        private Mock<IOutNetworkChannel> AddChannel(ChannelID channelID)
        {
            var channel = CreateChannel();

            outConnection.AddChannel(channelID, channel.Object);

            return channel;
        }

        private Mock<IOutNetworkChannel> CreateChannel()
        {
            var channel = new Mock<IOutNetworkChannel>(MockBehavior.Strict);
            channel.Setup(c => c.HasChanges(It.IsAny<IReadOnlyCollection<Entity>>())).Returns(false);
            channel.Setup(c => c.Serialize(It.IsAny<SerializerContext<IOutBitStream>>(), It.IsAny<AbsoluteSimulationFrame>(), It.IsAny<bool>(), It.IsAny<IReadOnlyCollection<Entity>>()));
            channel.Setup(c => c.MarkAsSent(It.IsAny<SequenceId>())).Returns((Dictionary<Entity, OutgoingEntityUpdate>)null);

            return channel;
        }

        private void Tick()
        {
            outConnection.Update(simulationFrame);
        }

        private List<OutPacket> GetSentPackets()
        {
            var packets = fakeConnection.SentPackets;
            fakeConnection.ClearSentPackets();

            return packets;
        }

        private void Assert_PacketHeaders(OutPacket outPacket, AbsoluteSimulationFrame expectedSimulationFrame, Vector3d expectedFloatingOrigin)
        {
            var octetStream = new InOctetStream(outPacket.Stream.Close().ToArray());

            Assert.That(octetStream != null);

            var basicHeader = PacketHeaderReader.DeserializeBasicHeader(octetStream);
            Assert.That(basicHeader.SimulationFrame, Is.EqualTo(expectedSimulationFrame));

            var decoded = PacketHeaderReader.ToPacketHeaderInfo(octetStream, basicHeader);
            var bitStream = decoded.Stream;

            var floatingOrigin = Deserialize.ReadFloatingOrigin(bitStream, logger);
            Assert.That(floatingOrigin, Is.EqualTo(expectedFloatingOrigin));
        }

        private void Assert_PacketChannelID(OutPacket outPacket, ChannelID expectedChannelID)
        {
            var octetStream = new InOctetStream(outPacket.Stream.Close().ToArray());

            Assert.That(octetStream != null);

            var basicHeader = PacketHeaderReader.DeserializeBasicHeader(octetStream);
            var decoded = PacketHeaderReader.ToPacketHeaderInfo(octetStream, basicHeader);
            var bitStream = decoded.Stream;
            Deserialize.ReadFloatingOrigin(bitStream, logger);

            Deserialize.ReadChannelID(bitStream, out var channelID);
            Assert.That(channelID, Is.EqualTo(expectedChannelID));
        }

        private class FakeConnection : IOutConnection
        {
            public bool CanSend => true;
            public bool UseDebugStreams => false;

            public int PacketSize;
            public List<OutPacket> SentPackets;

            readonly public Logger Logger;

            public FakeConnection(Logger logger)
            {
                PacketSize = Brisk.DefaultMTU;
                SentPackets = new List<OutPacket>();
                Logger = logger;
            }

            public OutPacket CreatePacket(bool reliable)
            {
                var stream = new OutOctetStream(PacketSize);

                return new OutPacket(stream, new SequenceId(0), true, false, Logger);
            }

            public void Send(OutPacket outPacket)
            {
                SentPackets.Add(outPacket);
            }

            public void ClearSentPackets()
            {
                SentPackets = new List<OutPacket>();
            }
        }
    }
}

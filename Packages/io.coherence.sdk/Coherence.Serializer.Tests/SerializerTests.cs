// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

// If these tests fail, then it's likely the code gen is broken.
// Take a look at README_BAKING in Coherence.Common.Tests to regenerate the code.

namespace Coherence.Serializer.Tests
{
    using System;
    using System.Collections.Generic;
    using Brook;
    using Brook.Octet;
    using Entities;
    using Generated;
    using Log;
    using Moq;
    using NUnit.Framework;
    using ProtocolDef;
    using SimulationFrame;
    using Coherence.Tests;
    using Connection;

    public class SerializerTest : CoherenceTest
    {
        private Mock<ISchemaSpecificComponentSerialize> componentSerializerMock;
        private Mock<Logger> loggerMock;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            componentSerializerMock = new Mock<ISchemaSpecificComponentSerialize>();
            loggerMock = new Mock<Logger>(null, null, null);
        }

        [TestCase(8, 0)]
        [TestCase(8, 1)]
        [TestCase(16, 0)]
        [TestCase(16, 5)]
        [TestCase(16, 16)]
        [TestCase(64, 0)]
        [TestCase(64, 5)]
        [TestCase(64, 64)]
        [TestCase(1024, 0)]
        [TestCase(1024, 20)]
        [TestCase(1024, 40)]
        [TestCase(1024, 1024)]
        public void SerializeUpdated_ShouldCorrectlyMeasureBitsTaken(int bufferSizeBits, int bufferHeaderSizeBits)
        {
            // Arrange
            componentSerializerMock
                .Setup(o => o.WriteComponentUpdate(It.IsAny<ICoherenceComponentData>(), It.IsAny<uint>(), It.IsAny<bool>(),
                    It.IsAny<AbsoluteSimulationFrame>(), It.IsAny<IOutProtocolBitStream>(), It.IsAny<Logger>()))
                .Callback<ICoherenceComponentData, uint, bool, AbsoluteSimulationFrame, IOutProtocolBitStream, Logger>(
                    (_1, _2, _3, _4, bitStream, _5) => bitStream.WriteLong(long.MaxValue));

            var change = new EntityChange
            {
                Update = new OutgoingEntityUpdate
                {
                    Operation = EntityOperation.Create,
                    Components = DeltaComponents.New()
                }
            };

            change.Update.Components.UpdateComponent(ComponentChange.New(new WorldPosition()));
            change.Update.Components.RemoveComponent(3);

            var octetWriter = new OutOctetStream(bufferSizeBits / 8);
            var bitStream = new OutBitStream(octetWriter);
            WriteBitsToBitStream(bitStream, bufferHeaderSizeBits);

            ushort lastIndex = 0;

            // Act
            Serialize.SerializeUpdated(change, new AbsoluteSimulationFrame(), componentSerializerMock.Object,
                new SerializerContext<IOutBitStream>(bitStream, false, loggerMock.Object), ref lastIndex, out uint bitsTaken);

            // Assert
            bitStream.Flush();

            var expectedRemainingSize = bufferSizeBits - bufferHeaderSizeBits - bitsTaken;

            if (expectedRemainingSize > 0)
            {
                Assert.AreEqual(0, bitStream.OverflowBitCount);
                Assert.AreEqual(expectedRemainingSize, bitStream.RemainingBitCount);
                Assert.IsFalse(bitStream.IsFull);
            }
            else
            {
                var expectedOverflowBitCount = bitsTaken + bufferHeaderSizeBits - bufferSizeBits;
                Assert.AreEqual(expectedOverflowBitCount, bitStream.OverflowBitCount);
                Assert.IsTrue(bitStream.IsFull);
            }
        }

        [Test]
        [Description("Tests that a component with no fields can be deserialized into an entity update.")]
        public void DeserializeComponentWithNoFields()
        {
            // Arrange
            var root = new Definition();
            var logger = Log.GetLogger<SerializerTest>();

            var change = new EntityChange
            {
                Update = new OutgoingEntityUpdate
                {
                    Operation = EntityOperation.Create,
                    Components = DeltaComponents.New()
                }
            };

            // add a component with no fields like the global component.
            change.Update.Components.UpdateComponent(ComponentChange.New(new Global()));

            var stream = new Brook.Octet.OutOctetStream(1280);
            var bitStream = new OutBitStream(stream);
            var lastIndex = (ushort)0;
            var bitsTaken = (uint)0;
            Serialize.SerializeUpdated(change, 1, root, new SerializerContext<IOutBitStream>(bitStream, false, logger), ref lastIndex, out bitsTaken);

            bitStream.Flush();

            var outBuffer = stream.Close().ToArray();
            var octetReader = new InOctetStream(outBuffer);
            var inBitStream = new InBitStream(octetReader, outBuffer.Length * 8);

            // Act
            var readEntity = Deserialize.ReadEntity(inBitStream, 1, ref lastIndex, out EntityWithMeta meta, out AbsoluteSimulationFrame entityRefSimFrame, logger);
            var entityUpdate = IncomingEntityUpdate.New();
            entityUpdate = Deserialize.UpdateComponents(root, entityUpdate, entityRefSimFrame, inBitStream, root, logger);

            // Assert
            Assert.That(readEntity, Is.True);
            Assert.That(entityUpdate.Components.Count, Is.EqualTo(1));
        }

        [Test(Description = "Serialization and deserialization of a list of ordered commands")]
        public void SerializeOrderedCommands()
        {
            var definition = new Definition();
            var commands = new List<(MessageID, SerializedEntityMessage)>()
            {
                (new MessageID(5), CreateTestCommand(definition)),
                (new MessageID(6), CreateTestCommand(definition)),
                (new MessageID(9), CreateTestCommand(definition)),
                (new MessageID(10), CreateTestCommand(definition))
            };

            var stream = new OutOctetStream(1280);

            // Serialize
            {
                var ctx = new SerializerContext<IOutBitStream>(new OutBitStream(stream), false, logger);

                var res = Serialize.WriteOrderedCommands(commands, ctx);
                Serialize.WriteEndOfMessages(ctx);
                ctx.BitStream.Flush();

                Assert.That(res.Count, Is.EqualTo(commands.Count));
                for (var i = 0; i < res.Count; ++i)
                {
                    Assert.That(res[i], Is.EqualTo(commands[i].Item1));
                }
            }

            // Deserialize
            {
                var octetReader = new InOctetStream(stream.Close().ToArray());
                var inBitStream = new InBitStream(octetReader, (int)octetReader.Length * 8);
                var res = Deserialize.ReadOrderedCommands(inBitStream, definition, logger);

                Assert.That(res.Count, Is.EqualTo(commands.Count));
                for (var i = 0; i < res.Count; ++i)
                {
                    Assert.That(res[i].Item1, Is.EqualTo(commands[i].Item1));
                    Assert.That(res[i].Item2, Is.TypeOf<AuthorityRequest>());
                }
            }
        }

        private void WriteBitsToBitStream(IOutBitStream stream, int bitCount)
        {
            while (bitCount > 0)
            {
                stream.WriteBits(1, Math.Min(bitCount, 32));
                bitCount -= 32;
            }
        }

        private SerializedEntityMessage CreateTestCommand(Definition definition)
        {
            var entityID = new Entity(1, 0, false);
            var cmd = definition.CreateAuthorityRequest(
                entityID,
                new ClientID(1),
                AuthorityType.State
            );

            return Serialize.SerializeMessage(
                MessageType.Command,
                MessageTarget.AuthorityOnly,
                cmd,
                entityID,
                definition,
                false,
                logger
            );
        }
    }
}

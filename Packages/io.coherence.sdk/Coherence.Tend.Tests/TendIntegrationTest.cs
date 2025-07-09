namespace Coherence.Tend.Tests
{
    using Coherence.Brook;
    using Coherence.Brook.Octet;
    using Coherence.Log;
    using Coherence.Tend.Client;
    using Coherence.Tend.Models;
    using Moq;
    using NUnit.Framework;
    using System.Collections.Generic;

    public class TendIntegrationTest
    {
        private class Client
        {
            public Tend Tend { get; set; }
            public Mock<Logger> LoggerMock { get; set; }

            public List<DeliveryInfo> DeliveryInfos = new List<DeliveryInfo>();

            public Client()
            {
                LoggerMock = new Mock<Logger>(null, null, null);
                _ = LoggerMock.Setup(m => m.With<It.IsAnyType>()).Returns(() => LoggerMock.Object);

                Tend = new Tend(LoggerMock.Object, new OutgoingLogic(LoggerMock.Object), new IncomingLogic(LoggerMock.Object));
                Tend.OnDeliveryInfo += OnDeliveryInfo;
                Tend.Connected = true;
            }

            private void OnDeliveryInfo(DeliveryInfo info)
            {
                this.DeliveryInfos.Add(info);
            }

            public (TendHeader header, IInOctetStream octetStream) WriteHeader(bool reliable = true)
            {
                var outStream = new OutOctetStream(2048);
                var header = Tend.WriteHeader(outStream, reliable);

                Tend.OnPacketSent(Tend.OutgoingSequenceId, reliable);

                return (header, new InOctetStream(outStream.Close().ToArray()));
            }
        }

        private Client client1;
        private Client client2;

        [SetUp]
        public void Setup()
        {
            client1 = new Client();
            client2 = new Client();
        }

        [Test]
        public void NotReliable()
        {
            // Tests if sending non-reliable packets is possible without any limitations.

            // Arrange
            var repetitions = 1000;

            // Act

            // Client1 -> Client2
            for (int i = 0; i < repetitions; i++)
            {
                var (expectedHeader, stream) = client1.WriteHeader(false);
                Assert.True(client2.Tend.ReadHeader(stream, out var header, out var didAck));

                Assert.AreEqual(expectedHeader, header);
                Assert.That(didAck, Is.False);
            }

            // Client2 -> Client1
            for (int i = 0; i < repetitions; i++)
            {
                var (expectedHeader, stream) = client2.WriteHeader(false);
                Assert.True(client1.Tend.ReadHeader(stream, out var header, out var didAck));

                Assert.AreEqual(expectedHeader, header);
                Assert.That(didAck, Is.False);
            }

            // Assert
            Assert.AreEqual(0, client1.DeliveryInfos.Count);
            Assert.AreEqual(0, client2.DeliveryInfos.Count);
        }

        [Test]
        public void InOrder()
        {
            // Tests if sending packets in order correctly creates DeliveryInfos.

            // Arrange
            var repetitions = 1000;

            // Act
            for (int i = 0; i < repetitions; i++)
            {
                // Client1 -> Client2
                {
                    var (expectedHeader, stream) = client1.WriteHeader();
                    Assert.True(client2.Tend.ReadHeader(stream, out var header, out var didAck));

                    Assert.AreEqual(expectedHeader, header);
                    if (i == 0)
                    {
                        Assert.That(didAck, Is.False);
                    }
                    else
                    {
                        Assert.That(didAck, Is.True);
                    }
                }

                // Client2 -> Client1
                {
                    var (expectedHeader, stream) = client2.WriteHeader();
                    Assert.True(client1.Tend.ReadHeader(stream, out var header, out var didAck));

                    Assert.AreEqual(expectedHeader, header);
                    Assert.That(didAck, Is.True);
                }
            }

            // Assert
            Assert.AreEqual(repetitions, client1.DeliveryInfos.Count);
            Assert.AreEqual(repetitions - 1, client2.DeliveryInfos.Count); // -1 because last packet from Client2 wasn't confirmed by Client1
        }

        [Test]
        public void OutOfOrder()
        {
            // Tests if a packet is received out of order that it is treated as not delivered.

            // Arrange
            var expectedDelivery = new List<DeliveryInfo>()
            {
                new DeliveryInfo() { PacketSequenceId = new SequenceId(0), WasDelivered = true },
                new DeliveryInfo() { PacketSequenceId = new SequenceId(1), WasDelivered = false },
                new DeliveryInfo() { PacketSequenceId = new SequenceId(2), WasDelivered = true },
            };

            // Act
            var (_, stream0) = client1.WriteHeader();
            var (_, stream1) = client1.WriteHeader();
            var (_, stream2) = client1.WriteHeader();

            Assert.True(client2.Tend.ReadHeader(stream0, out var _, out var _));
            Assert.True(client2.Tend.ReadHeader(stream2, out var _, out var _));
            Assert.False(client2.Tend.ReadHeader(stream1, out var _, out var _)); // out of order should be ignored

            // Notify client1 about client2 state
            var (_, stream) = client2.WriteHeader();
            Assert.True(client1.Tend.ReadHeader(stream, out var _, out var _));

            // Assert
            Assert.AreEqual(expectedDelivery, client1.DeliveryInfos);
        }

        [Test]
        public void Dropped()
        {
            // Tests if a packet is dropped that correct DeliveryInfos are generated.

            // Arrange
            var expectedDelivery = new List<DeliveryInfo>()
            {
                new DeliveryInfo() { PacketSequenceId = new SequenceId(0), WasDelivered = true },
                new DeliveryInfo() { PacketSequenceId = new SequenceId(1), WasDelivered = false },
                new DeliveryInfo() { PacketSequenceId = new SequenceId(2), WasDelivered = true },
            };

            // Act
            var (_, stream0) = client1.WriteHeader();
            _ = client1.WriteHeader();
            var (_, stream2) = client1.WriteHeader();

            Assert.True(client2.Tend.ReadHeader(stream0, out var _, out var _));
            // client2.Tend.ReadHeader(stream1); // dropped
            Assert.True(client2.Tend.ReadHeader(stream2, out var _, out var _));

            // Notify client1 about client2 state
            var (_, stream) = client2.WriteHeader();
            Assert.True(client1.Tend.ReadHeader(stream, out var _, out var _));

            // Assert
            Assert.AreEqual(expectedDelivery, client1.DeliveryInfos);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(32)]
        public void DroppingStream(int noDropFactor)
        {
            // Tests if long lasting stream with dropping packets still correctly generates DeliveryInfos

            // Arrange
            var repetitions = 1000;
            var expectedDeliveryInfos = new List<DeliveryInfo>();

            // Act
            for (int i = 0; i < repetitions; i++)
            {
                var dropped = i % noDropFactor != 0;

                // Client1 -> Client2
                {
                    var (header, stream) = client1.WriteHeader();
                    expectedDeliveryInfos.Add(new DeliveryInfo()
                    {
                        PacketSequenceId = header.packetId,
                        WasDelivered = !dropped
                    });

                    if (!dropped)
                    {
                        Assert.True(client2.Tend.ReadHeader(stream, out var _, out var didAck));

                        if (i == 0)
                        {
                            Assert.That(didAck, Is.False);
                        }
                        else
                        {
                            Assert.That(didAck, Is.True);
                        }
                    }
                }

                // Client2 -> Client1
                {
                    var (_, stream) = client2.WriteHeader();
                    Assert.True(client1.Tend.ReadHeader(stream, out var _, out var didAck));

                    Assert.AreEqual(!dropped, didAck);
                }

                // Assert
                if (!dropped) // because client2 doesn't know about dropped packets until next packet arrives
                {
                    Assert.AreEqual(expectedDeliveryInfos, client1.DeliveryInfos);

                    client1.DeliveryInfos.Clear();
                    expectedDeliveryInfos.Clear();
                }
            }
        }
    }
}

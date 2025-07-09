// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport.Tests
{
    using Brook;
    using Brook.Octet;
    using Common;
    using Connection;
    using Log;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Coherence.Tests;

    public class TransportConditionerTests : CoherenceTest
    {
        [TestCase(1f, 0f, true)]
        [TestCase(1f, 1f, true)]
        [TestCase(0f, 0f, false)]
        [TestCase(0f, 1f, false)]
        [TestCase(0.5f, 0.5f, true)]
        [TestCase(0.5f, 0.51f, false)]
        public void Send_DropsPackets(float dropRate, float randomResult, bool packetDropped)
        {
            // Arrange
            TransportConditioner conditioner = GetTestTransportConditioner(out Mock<ITransport> transportMock, out _);
            conditioner.Config.Conditions = new Condition
            {
                sendDropRate = dropRate
            };

            var randomMock = new Mock<IRandom>();
            randomMock.Setup(m => m.NextDouble())
                .Returns(randomResult);

            conditioner.Config.Random = randomMock.Object;

            // Act
            conditioner.Send(new OutOctetStream(2048));

            // Assert
            Func<Times> sentTimes = packetDropped ? (Func<Times>)Times.Never : Times.Once;
            transportMock.Verify(m => m.Send(It.IsAny<OutOctetStream>()), sentTimes);
        }

        [TestCase(1f, 0f, true)]
        [TestCase(1f, 1f, true)]
        [TestCase(0f, 0f, false)]
        [TestCase(0f, 1f, false)]
        [TestCase(0.5f, 0.5f, true)]
        [TestCase(0.5f, 0.51f, false)]
        public void Receive_DropsPackets(float dropRate, float randomResult, bool packetDropped)
        {
            // Arrange
            TransportConditioner conditioner = GetTestTransportConditioner(out Mock<ITransport> transportMock, out _);
            conditioner.Config.Conditions = new Condition
            {
                receiveDropRate = dropRate
            };

            transportMock.Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) => buffer.Add((new InOctetStream(Array.Empty<byte>()), null)));

            var randomMock = new Mock<IRandom>();
            randomMock.Setup(m => m.NextDouble())
                .Returns(randomResult);

            conditioner.Config.Random = randomMock.Object;

            var packetBuffer = new List<(IInOctetStream, IPEndPoint)>();

            // Act
            conditioner.Receive(packetBuffer);

            // Assert
            transportMock.Verify(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()), Times.Once);
            Assert.That(packetBuffer, packetDropped ? Is.Empty : Is.Not.Empty);
        }


        [Test]
        public void Send_DelaysPackets()
        {
            // Arrange
            TransportConditioner conditioner = GetTestTransportConditioner(
                out Mock<ITransport> transportMock,
                out Mock<IDateTimeProvider> dateTimeProviderMock);

            float delaySec = 1f;

            conditioner.Config.Conditions = new Condition
            {
                sendDelaySec = delaySec
            };

            List<IOutOctetStream> packetsToSend = GetTestOutPackets(3);

            List<IOutOctetStream> sentPackets = new List<IOutOctetStream>();
            transportMock.Setup(m => m.Send(It.IsAny<IOutOctetStream>()))
                .Callback((IOutOctetStream packet) => sentPackets.Add(packet));

            // Act
            for (int i = 0; i < packetsToSend.Count; i++)
            {
                IOutOctetStream packet = packetsToSend[i];
                conditioner.Config.Conditions = new Condition
                {
                    sendDelaySec = delaySec * (i + 1)
                };

                conditioner.Send(packet);
            }

            // Assert
            transportMock.Verify(m => m.Send(It.IsAny<OutOctetStream>()), Times.Never);

            DateTime utcNow = default;
            for (int i = 0; i < packetsToSend.Count; i++)
            {
                utcNow += TimeSpan.FromSeconds(delaySec);
                dateTimeProviderMock.SetupGet(m => m.UtcNow)
                    .Returns(utcNow);

                conditioner.Receive(new List<(IInOctetStream, IPEndPoint)>());

                transportMock.Verify(m => m.Send(It.IsAny<OutOctetStream>()), Times.Once);
                transportMock.Invocations.Clear();
            }

            CollectionAssert.AreEqual(packetsToSend, sentPackets);
        }

        [Test]
        public void Receive_DelaysPackets()
        {
            // Arrange
            TransportConditioner conditioner = GetTestTransportConditioner(
                out Mock<ITransport> transportMock,
                out Mock<IDateTimeProvider> dateTimeProviderMock);

            float delaySec = 1f;

            conditioner.Config.Conditions = new Condition
            {
                receiveDelaySec = delaySec
            };

            int receiveOperation = 0;
            List<IInOctetStream> packetsToReceive = GetTestInPackets(3);

            transportMock.Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) =>
                {
                    if (receiveOperation < packetsToReceive.Count)
                    {
                        buffer.Add((packetsToReceive[receiveOperation++], null));
                    }
                });

            // Act
            List<(IInOctetStream stream, IPEndPoint)> packetBuffer = new List<(IInOctetStream, IPEndPoint)>();
            for (int i = 0; i < packetsToReceive.Count; i++)
            {
                conditioner.Config.Conditions = new Condition
                {
                    receiveDelaySec = delaySec * (i + 1)
                };

                conditioner.Receive(packetBuffer);
            }

            // Assert
            Assert.That(packetBuffer, Is.Empty);

            DateTime utcNow = default;
            for (int i = 0; i < packetsToReceive.Count; i++)
            {
                utcNow += TimeSpan.FromSeconds(delaySec);
                dateTimeProviderMock.SetupGet(m => m.UtcNow)
                    .Returns(utcNow);

                conditioner.Receive(packetBuffer);

                Assert.That(packetBuffer.Count, Is.EqualTo(i + 1));
            }

            CollectionAssert.AreEqual(packetsToReceive, packetBuffer.Select(p => p.stream));
        }

        [Test]
        public void HoldOutgoingPackets_Works()
        {
            // Arrange
            TransportConditioner conditioner = GetTestTransportConditioner(out Mock<ITransport> transportMock, out _);
            conditioner.Config.HoldOutgoingPackets = true;

            List<IOutOctetStream> sentPackets = new List<IOutOctetStream>();

            transportMock.Setup(m => m.Send(It.IsAny<IOutOctetStream>()))
                .Callback((IOutOctetStream packet) => sentPackets.Add(packet));

            List<IOutOctetStream> packetsToSend = GetTestOutPackets(3);

            // Act & Assert
            foreach (var packet in packetsToSend)
            {
                conditioner.Send(packet);
            }
            transportMock.Verify(m => m.Send(It.IsAny<OutOctetStream>()), Times.Never);

            conditioner.ReleaseAllHeldOutgoingPackets();

            transportMock.Verify(m => m.Send(It.IsAny<OutOctetStream>()), Times.Exactly(packetsToSend.Count));
            CollectionAssert.AreEqual(packetsToSend, sentPackets);
        }

        [TestCase(1f, 0f, true)]
        [TestCase(1f, 1f, true)]
        [TestCase(0f, 0f, false)]
        [TestCase(0f, 1f, false)]
        [TestCase(0.5f, 0.5f, true)]
        [TestCase(0.5f, 0.51f, false)]
        public void TamperOutgoingPacket_TampersPackets(float packetTamperRate, float randomResult, bool shouldTamper)
        {
            // Arrange
            TransportConditioner conditioner = GetTestTransportConditioner(out Mock<ITransport> transportMock, out _);
            conditioner.Config.Conditions = new Condition
            {
                packetTamperRate = packetTamperRate,
                tamperRate = 1f
            };

            var packet = GetRandomPacket(100);
            var packetOctets = packet.Octets.ToArray();

            IOutOctetStream sentPacket = null;
            transportMock.Setup(t => t.Send(It.IsAny<IOutOctetStream>())).Callback<IOutOctetStream>(p => sentPacket = p);

            var randomMock = new Mock<IRandom>();
            randomMock.Setup(m => m.NextDouble())
                .Returns(randomResult);

            conditioner.Config.Random = randomMock.Object;

            // Act
            conditioner.Send(packet);

            // Assert
            transportMock.Verify(m => m.Send(It.IsAny<OutOctetStream>()), Times.Once);

            Assert.That(sentPacket, Is.Not.Null);
            Assert.That(sentPacket.Octets.Length, Is.EqualTo(packet.Octets.Length));

            var sentPacketOctets = sentPacket.Octets.ToArray();

            for (int i = 0; i < packetOctets.Length; i++)
            {
                if (shouldTamper)
                {
                    Assert.That(sentPacketOctets[i], Is.EqualTo((byte)~packetOctets[i]));
                }
                else
                {
                    Assert.That(sentPacketOctets[i], Is.EqualTo(packetOctets[i]));
                }
            }
        }

        [TestCase(1f, 0f, true)]
        [TestCase(1f, 1f, true)]
        [TestCase(0f, 0f, false)]
        [TestCase(0f, 1f, false)]
        [TestCase(0.5f, 0.5f, true)]
        [TestCase(0.5f, 0.51f, false)]
        public void TamperOutgoingPacket_TampersBits(float tamperRate, float randomResult, bool shouldTamper)
        {
            // Arrange
            TransportConditioner conditioner = GetTestTransportConditioner(out Mock<ITransport> transportMock, out _);
            conditioner.Config.Conditions = new Condition
            {
                packetTamperRate = 1f,
                tamperRate = tamperRate
            };

            var packet = GetRandomPacket(100);
            var packetOctets = packet.Octets.ToArray();

            IOutOctetStream sentPacket = null;
            transportMock.Setup(t => t.Send(It.IsAny<IOutOctetStream>())).Callback<IOutOctetStream>(p => sentPacket = p);

            var tamperBitMod = 3;
            var randomIndex = -1; // Start at -1 because the first NextDouble call is used to determine if the packet should be tampered

            var randomMock = new Mock<IRandom>();
            randomMock.Setup(m => m.NextDouble())
                .Returns(() =>
                {
                    if (randomIndex == -1)
                    {
                        randomIndex++;
                        return 1f;
                    }

                    var shouldTamper = randomIndex % tamperBitMod == 0;

                    randomIndex++;

                    return shouldTamper ? randomResult : tamperRate + 0.1f;
                });

            conditioner.Config.Random = randomMock.Object;

            // Act
            conditioner.Send(packet);

            // Assert
            transportMock.Verify(m => m.Send(It.IsAny<OutOctetStream>()), Times.Once);

            Assert.That(sentPacket, Is.Not.Null);
            Assert.That(sentPacket.Octets.Length, Is.EqualTo(packet.Octets.Length));

            var sentPacketOctets = sentPacket.Octets.ToArray();

            for (int i = 0; i < packetOctets.Length; i++)
            {
                if (!shouldTamper)
                {
                    Assert.That(sentPacketOctets[i], Is.EqualTo(packetOctets[i]));
                    continue;
                }

                var octet = packetOctets[i];
                var sentOctet = sentPacketOctets[i];

                for (int j = 0; j < 8; j++)
                {
                    var shouldTamperBit = (i * 8 + j) % tamperBitMod == 0;

                    if (shouldTamperBit)
                    {
                        Assert.That(sentOctet & (1 << j), Is.Not.EqualTo(octet & (1 << j)));
                    }
                    else
                    {
                        Assert.That(sentOctet & (1 << j), Is.EqualTo(octet & (1 << j)));
                    }
                }
            }
        }

        [Test]
        public void TamperOutgoingPacket_TampersPacketsAtStartPoint()
        {
            // Arrange
            var tamperStart = 0.4f;

            TransportConditioner conditioner = GetTestTransportConditioner(out Mock<ITransport> transportMock, out _);
            conditioner.Config.Conditions = new Condition
            {
                packetTamperRate = 1f,
                tamperRate = 1f,
                tamperStart = tamperStart,
            };

            var packet = GetRandomPacket(100);
            var packetOctets = packet.Octets.ToArray();

            IOutOctetStream sentPacket = null;
            transportMock.Setup(t => t.Send(It.IsAny<IOutOctetStream>())).Callback<IOutOctetStream>(p => sentPacket = p);

            var randomMock = new Mock<IRandom>();
            randomMock.Setup(m => m.NextDouble()).Returns(0f);
            randomMock.Setup(m => m.NextNormalDistribution(tamperStart, It.IsAny<double>())).Returns(tamperStart);

            conditioner.Config.Random = randomMock.Object;

            // Act
            conditioner.Send(packet);

            // Assert
            transportMock.Verify(m => m.Send(It.IsAny<OutOctetStream>()), Times.Once);

            Assert.That(sentPacket, Is.Not.Null);
            Assert.That(sentPacket.Octets.Length, Is.EqualTo(packet.Octets.Length));

            var sentPacketOctets = sentPacket.Octets.ToArray();

            for (int i = 0; i < packetOctets.Length; i++)
            {
                if (i < packetOctets.Length * tamperStart)
                {
                    Assert.That(sentPacketOctets[i], Is.EqualTo(packetOctets[i]));
                }
                else
                {
                    Assert.That(sentPacketOctets[i], Is.EqualTo((byte)~packetOctets[i]));
                }
            }
        }

        private static List<IOutOctetStream> GetTestOutPackets(int count)
        {
            List<IOutOctetStream> packets = new List<IOutOctetStream>();
            for (int i = 0; i < count; i++)
            {
                packets.Add(new OutOctetStream(2048));
            }

            return packets;
        }

        private static List<IInOctetStream> GetTestInPackets(int count)
        {
            List<IInOctetStream> packets = new List<IInOctetStream>();
            for (int i = 0; i < count; i++)
            {
                packets.Add(new InOctetStream(Array.Empty<byte>()));
            }

            return packets;
        }

        private static OutOctetStream GetRandomPacket(int size)
        {
            var random = new Random(1234);

            var bytes = new byte[size];
            random.NextBytes(bytes);

            var stream = new OutOctetStream(bytes);
            stream.WriteOctets(bytes);

            return stream;
        }

        private static TransportConditioner GetTestTransportConditioner(
        out Mock<ITransport> transportMock,
        out Mock<IDateTimeProvider> dateTimeProviderMock)
        {
            transportMock = new Mock<ITransport>();
            dateTimeProviderMock = new Mock<IDateTimeProvider>();

            return new TransportConditioner(transportMock.Object,
                dateTimeProviderMock.Object,
                Log.GetLogger<TransportConditioner>());
        }
    }
}

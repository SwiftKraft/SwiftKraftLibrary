namespace Coherence.Tend.Tests
{
    using Brook.Octet;
    using Client;
    using Coherence.Brook;
    using Coherence.Tend.Models;
    using Log;
    using Moq;
    using NUnit.Framework;
    using NUnit.Framework.Constraints;
    using System.Collections.Generic;

    public partial class TendTests
    {
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void ReadHeader_ShouldReturnTendHeader(bool isReliable, bool isSuccessor)
        {
            // Arrange
            var expectedHeader = new TendHeader()
            {
                isReliable = isReliable,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var inStream = SerializeHeader(expectedHeader);

            incomingLogicMock.Setup(o => o.ReceivedToUs(It.IsAny<SequenceId>())).Returns(isSuccessor);

            // Act
            var isValid = tend.ReadHeader(inStream, out var header, out var _);

            // Assert
            if (!isReliable)
            {
                Assert.AreEqual(isValid, true, "Non-reliable header should always be valid");
                Assert.AreEqual(isReliable, header.isReliable, "Returned header should be correct");
            }
            else
            {
                Assert.AreEqual(isValid, isSuccessor, "Reliable header should be valid only when isSuccessor");
                Assert.AreEqual(expectedHeader, header, "Returned header should be correct");
            }
        }

        [Test]
        public void ReadHeader_WhenNotReliable_ShouldNotCallInAndOutLogic()
        {
            // Arrange
            var expectedHeader = new TendHeader()
            {
                isReliable = false,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var inStream = SerializeHeader(expectedHeader);

            // Act
            Assert.True(tend.ReadHeader(inStream, out var _, out var didAck));

            // Assert
            incomingLogicMock.Verify(o => o.ReceivedToUs(It.IsAny<SequenceId>()), Times.Never,
                "When not reliable, tend shouldn't call ReceivedToUs");
            outgoingLogicMock.Verify(o => o.ReceivedByRemote(It.IsAny<SequenceId>(), It.IsAny<ReceiveMask>()), Times.Never,
                "When not reliable, tend shouldn't call ReceivedByRemote");

            Assert.That(didAck, Is.False);
        }

        [Test]
        public void ReadHeader_WhenReliableAndSuccessor_ShouldCallInAndOutLogic()
        {
            // Arrange
            var expectedHeader = new TendHeader()
            {
                isReliable = true,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var inStream = SerializeHeader(expectedHeader);

            incomingLogicMock.Setup(o => o.ReceivedToUs(It.IsAny<SequenceId>())).Returns(true);

            // Act
            Assert.True(tend.ReadHeader(inStream, out var _, out var _));

            // Assert
            incomingLogicMock.Verify(o => o.ReceivedToUs(expectedHeader.packetId), Times.Once, "When reliable, tend should call ReceivedToUs");
            outgoingLogicMock.Verify(o => o.ReceivedByRemote(expectedHeader.receivedId, expectedHeader.receiveMask), Times.Once, "When reliable, tend should call ReceivedByRemote");
        }

        [Test]
        public void ReadHeader_WhenReliableAndNotSuccessor_ShouldCallInAndNotOutLogic()
        {
            // Arrange
            var expectedHeader = new TendHeader()
            {
                isReliable = true,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var inStream = SerializeHeader(expectedHeader);

            incomingLogicMock.Setup(o => o.ReceivedToUs(It.IsAny<SequenceId>())).Returns(false);

            // Act
            Assert.False(tend.ReadHeader(inStream, out var _, out var _));

            // Assert
            incomingLogicMock.Verify(o => o.ReceivedToUs(expectedHeader.packetId), Times.Once,
                "When reliable, tend should call ReceivedToUs");
            outgoingLogicMock.Verify(o => o.ReceivedByRemote(It.IsAny<SequenceId>(), It.IsAny<ReceiveMask>()), Times.Never,
                "When reliable and not successor, tend shouldn't call ReceivedByRemote");
        }

        private static DeliveryInfo deliveryInfo1 = new DeliveryInfo() { PacketSequenceId = new SequenceId(12), WasDelivered = true };
        private static DeliveryInfo deliveryInfo2 = new DeliveryInfo() { PacketSequenceId = new SequenceId(12), WasDelivered = false };
        private static DeliveryInfo deliveryInfo3 = new DeliveryInfo() { PacketSequenceId = new SequenceId(18), WasDelivered = false };
        private static DeliveryInfo deliveryInfo4 = new DeliveryInfo() { PacketSequenceId = new SequenceId(18), WasDelivered = true };

        private static List<DeliveryInfo>[] deliveryInfoSource =
        {
            new List<DeliveryInfo>(),
            new List<DeliveryInfo>(new DeliveryInfo[] { deliveryInfo1 }),
            new List<DeliveryInfo>(new DeliveryInfo[] { deliveryInfo1, deliveryInfo2, deliveryInfo3, deliveryInfo4 })
        };

        [TestCaseSource(nameof(deliveryInfoSource))]
        public void ReadHeader_WhenReliableAndSuccessor_ShouldTriggerDeliveryInfo(List<DeliveryInfo> deliveryInfos)
        {
            // Arrange
            var expectedHeader = new TendHeader()
            {
                isReliable = true,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var inStream = SerializeHeader(expectedHeader);

            var triggeredDeliveries = new List<DeliveryInfo>();
            tend.OnDeliveryInfo += di =>
            {
                triggeredDeliveries.Add(di);
            };

            var currentIndex = 0;

            incomingLogicMock.Setup(o => o.ReceivedToUs(It.IsAny<SequenceId>())).Returns(true);
            outgoingLogicMock.Setup(o => o.ReceivedByRemote(It.IsAny<SequenceId>(), It.IsAny<ReceiveMask>())).Returns(true);
            outgoingLogicMock.Setup(o => o.Dequeue()).Callback(() => currentIndex++).Returns(() => deliveryInfos[currentIndex - 1]);
            outgoingLogicMock.SetupGet(o => o.Count).Returns(() => deliveryInfos.Count - currentIndex);

            // Act
            Assert.True(tend.ReadHeader(inStream, out var _, out var didAck));

            // Assert
            outgoingLogicMock.Verify(o => o.Dequeue(), Times.Exactly(deliveryInfos.Count), "All delivery infos should be dequeued.");
            Assert.AreEqual(deliveryInfos, triggeredDeliveries, "All delivery infos should be triggered.");
            Assert.That(didAck, Is.True);
        }

        [TestCaseSource(nameof(deliveryInfoSource))]
        public void ReadHeader_WhenNotReliable_ShouldNotTriggerDeliveryInfo(List<DeliveryInfo> deliveryInfos)
        {
            // Arrange
            var expectedHeader = new TendHeader()
            {
                isReliable = false,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var inStream = SerializeHeader(expectedHeader);

            var triggeredDeliveries = new List<DeliveryInfo>();
            tend.OnDeliveryInfo += di =>
            {
                triggeredDeliveries.Add(di);
            };

            var currentIndex = 0;

            incomingLogicMock.Setup(o => o.ReceivedToUs(It.IsAny<SequenceId>())).Returns(true);
            outgoingLogicMock.Setup(o => o.ReceivedByRemote(It.IsAny<SequenceId>(), It.IsAny<ReceiveMask>())).Returns(true);
            outgoingLogicMock.Setup(o => o.Dequeue()).Callback(() => currentIndex++).Returns(() => deliveryInfos[currentIndex - 1]);
            outgoingLogicMock.SetupGet(o => o.Count).Returns(() => deliveryInfos.Count - currentIndex);

            // Act
            Assert.True(tend.ReadHeader(inStream, out var _, out var didAck));

            // Assert
            outgoingLogicMock.Verify(o => o.Dequeue(), Times.Never, "No delivery infos should be dequeued.");
            Assert.IsEmpty(triggeredDeliveries, "No delivery info should trigger");
            Assert.That(didAck, Is.False);
        }

        [TestCaseSource(nameof(deliveryInfoSource))]
        public void ReadHeader_WhenNotSuccessor_ShouldNotTriggerDeliveryInfo(List<DeliveryInfo> deliveryInfos)
        {
            // Arrange
            var expectedHeader = new TendHeader()
            {
                isReliable = true,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var inStream = SerializeHeader(expectedHeader);

            var triggeredDeliveries = new List<DeliveryInfo>();
            tend.OnDeliveryInfo += di =>
            {
                triggeredDeliveries.Add(di);
            };

            var currentIndex = 0;

            incomingLogicMock.Setup(o => o.ReceivedToUs(It.IsAny<SequenceId>())).Returns(false);
            outgoingLogicMock.Setup(o => o.ReceivedByRemote(It.IsAny<SequenceId>(), It.IsAny<ReceiveMask>())).Returns(false);
            outgoingLogicMock.Setup(o => o.Dequeue()).Callback(() => currentIndex++).Returns(() => deliveryInfos[currentIndex - 1]);
            outgoingLogicMock.SetupGet(o => o.Count).Returns(() => deliveryInfos.Count - currentIndex);

            // Act
            Assert.False(tend.ReadHeader(inStream, out var _, out var didAck));

            // Assert
            outgoingLogicMock.Verify(o => o.Dequeue(), Times.Never, "No delivery infos should be dequeued.");
            Assert.IsEmpty(triggeredDeliveries, "No delivery info should trigger");
            Assert.That(didAck, Is.False);
        }

        [Description("Verifies that when the distance between last received remote sequence and just " +
                     "received remote sequence is bigger than the mask size then an error is returned.")]
        [TestCase(ReceiveMask.Range, true)]
        [TestCase(ReceiveMask.Range + 1, false)]
        public void DoesntAcceptRemoteSeqOutOfRange(byte remoteIdGap, bool expectReadOk)
        {
            // Arrange
            tend = new Tend(new UnityLogger());
            tend.Connected = true;

            tend.WriteHeader(new OutOctetStream(2048), true);

            tend.ReadHeader(SerializeHeader(0, 0, 0b1), out var _, out var _);

            // Act & Assert
            Assert.That(() =>
            {
                tend.ReadHeader(SerializeHeader(1, remoteIdGap, 0b1), out var _, out var _);
            }, expectReadOk ? (IResolveConstraint)Throws.Nothing : Throws.Exception);
        }

        [Test]
        [Description("Verifies that when the connection isn't set that reliable packets are not processed by the header.")]
        public void ReadHeader_WhenDisconnected_ReliableAreInvalid()
        {
            // Arrange
            tend.Connected = false;

            var expectedHeader = new TendHeader()
            {
                isReliable = true,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var inStream = SerializeHeader(expectedHeader);

            incomingLogicMock.Setup(o => o.ReceivedToUs(It.IsAny<SequenceId>())).Returns(true);

            // Act & Assert
            Assert.False(tend.ReadHeader(inStream, out var _, out var didAck));
        }

        [TestCaseSource(nameof(deliveryInfoSource))]
        [Description("Verifies that when the connection isn't set that the acks in a reliably delivered packet are" +
            "ignored since these are from a different connection.")]
        public void ReadHeader_WhenDisconnected_ShouldNotTriggerDeliveryInfo(List<DeliveryInfo> deliveryInfos)
        {
            // Arrange
            tend.Connected = false;

            var expectedHeader = new TendHeader()
            {
                isReliable = true,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var inStream = SerializeHeader(expectedHeader);

            var triggeredDeliveries = new List<DeliveryInfo>();
            tend.OnDeliveryInfo += di =>
            {
                triggeredDeliveries.Add(di);
            };

            var currentIndex = 0;

            incomingLogicMock.Setup(o => o.ReceivedToUs(It.IsAny<SequenceId>())).Returns(true);
            outgoingLogicMock.Setup(o => o.ReceivedByRemote(It.IsAny<SequenceId>(), It.IsAny<ReceiveMask>())).Returns(true);
            outgoingLogicMock.Setup(o => o.Dequeue()).Callback(() => currentIndex++).Returns(() => deliveryInfos[currentIndex - 1]);
            outgoingLogicMock.SetupGet(o => o.Count).Returns(() => deliveryInfos.Count - currentIndex);

            // Act
            Assert.False(tend.ReadHeader(inStream, out var _, out var didAck));

            // Assert
            outgoingLogicMock.Verify(o => o.Dequeue(), Times.Never, "No delivery infos should be dequeued.");
            Assert.IsEmpty(triggeredDeliveries, "No delivery info should trigger");
            Assert.That(didAck, Is.False);
        }

        [Test]
        [Description("Verifies that the packet is discarded if it does not have enough data.")]
        public void ReadHeader_WhenPacketTooSmall()
        {
            var logger = new Coherence.Common.Tests.TestLogger();
            tend = new Tend(logger);
            var data = new byte[] { };
            var instream = new InOctetStream(data);

            // not enough bytes to start processing, should return false immediately
            Assert.False(tend.ReadHeader(instream, out var _, out var didAck));
            Assert.AreEqual(logger.GetCountForWarningID(Warning.TendLessThan1Byte), 1);

            data = new byte[] { 1, 1 };
            instream = new InOctetStream(data);

            Assert.False(tend.ReadHeader(instream, out var _, out var didAck2));
            Assert.AreEqual(logger.GetCountForWarningID(Warning.TendInvalidPacket), 1);
        }
    }
}

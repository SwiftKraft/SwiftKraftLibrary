namespace Coherence.Tend.Tests
{
    using Client;
    using Coherence.Brook;
    using Coherence.Brook.Octet;
    using Coherence.Log;
    using Coherence.Tend.Models;
    using Moq;
    using NUnit.Framework;
    using Coherence.Tests;

    public partial class TendTests : CoherenceTest
    {
        private Tend tend;
        private Mock<IOutgoingLogic> outgoingLogicMock;
        private Mock<IIncomingLogic> incomingLogicMock;
        private Mock<Logger> loggerMock;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            outgoingLogicMock = new Mock<IOutgoingLogic>();
            incomingLogicMock = new Mock<IIncomingLogic>();
            loggerMock = new Mock<Logger>(null, null, null);

            tend = new Tend(loggerMock.Object, outgoingLogicMock.Object, incomingLogicMock.Object);
            tend.Connected = true;
        }

        public IInOctetStream SerializeHeader(TendHeader header)
        {
            var outStream = new OutOctetStream(2048);
            Tend.SerializeHeader(outStream, header);
            return new InOctetStream(outStream.Close().ToArray());
        }

        public IInOctetStream SerializeHeader(byte packetID, byte receivedID, byte receiveMask, bool reliable = true)
        {
            var header = new TendHeader
            {
                isReliable = reliable,
                packetId = new SequenceId(packetID),
                receivedId = new SequenceId(receivedID),
                receiveMask = new ReceiveMask(receiveMask)
            };

            return SerializeHeader(header);
        }
    }
}

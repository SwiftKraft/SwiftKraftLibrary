namespace Coherence.Tend.Tests
{
    using Client;
    using Coherence.Brook;
    using Coherence.Brook.Octet;
    using Coherence.Tend.Models;
    using NUnit.Framework;

    public partial class TendTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void SerializeDeserializeHeader_ShouldRemainEqual(bool isReliable)
        {
            // Arrange
            var expectedHeader = new TendHeader
            {
                isReliable = isReliable,
                packetId = new SequenceId(10),
                receivedId = new SequenceId(15),
                receiveMask = new ReceiveMask(20)
            };
            var outStream = new OutOctetStream(2048);

            // Act
            Tend.SerializeHeader(outStream, expectedHeader);
            var header = Tend.DeserializeHeader(new InOctetStream(outStream.Close().ToArray()));

            // Assert
            if (!isReliable)
            {
                Assert.AreEqual(isReliable, header.isReliable, "Headear should be correct.");
            }
            else
            {
                Assert.AreEqual(expectedHeader, header, "Header should be correct.");
            }
        }
    }
}

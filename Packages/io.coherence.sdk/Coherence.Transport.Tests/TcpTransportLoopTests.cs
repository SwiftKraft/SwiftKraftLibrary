// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport.Tests
{
    using Brook.Octet;
    using Connection;
    using Log;
    using Moq;
    using NUnit.Framework;
    using Stats;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ReceiveQueue = System.Collections.Concurrent.ConcurrentQueue<(byte[], Connection.ConnectionException)>;
    using SendQueue = Common.AsyncQueue<Brook.IOutOctetStream>;
    using Coherence.Tests;

    public sealed class TcpTransportLoopTests : CoherenceTest
    {
        private static readonly TimeSpan AsyncOperationTimeout = TimeSpan.FromSeconds(5);

        private Mock<Stream> streamMock;
        private ReceiveQueue receiveQueue;
        private SendQueue sendQueue;
        private Mock<IStats> statsMock;
        private CancellationTokenSource cancellationSource;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            streamMock = new Mock<Stream>();
            receiveQueue = new ReceiveQueue();
            sendQueue = new SendQueue();
            statsMock = new Mock<IStats>();
            cancellationSource = new CancellationTokenSource();
        }

        [TearDown]
        public override void TearDown()
        {
            cancellationSource.Cancel();

            base.TearDown();
        }

        private TcpTransportLoop CreateTestTcpTransportLoop() =>
            new(
                streamMock.Object,
                0,
                receiveQueue,
                sendQueue,
                statsMock.Object,
                new UnityLogger(),
                cancellationSource.Token
            );

        [Test]
        [Ignore("Disabled, see https://github.com/coherence/unity/issues/4631")]
        public async Task Receive_Works()
        {
            // Arrange
            var loop = CreateTestTcpTransportLoop();
            var ms = new MemoryStream();

            WritePacket("Hello", ms);
            WritePacket("World", ms);
            ms.Seek(0, SeekOrigin.Begin);

            streamMock
                .Setup(m => m.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] bytes, int _, int len, CancellationToken _) => Task.FromResult(ms.Read(bytes, 0, len)));

            // Act
            var run = loop.Run();

            // Assert
            var (ok, (data, _)) = await TryTakeFromReceiveQueue(AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data), Is.EqualTo("Hello"));

            (ok, (data, _)) = await TryTakeFromReceiveQueue(AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data), Is.EqualTo("World"));

            // Cleanup
            cancellationSource.Cancel();
            await run;
        }

        [Test]
        [Ignore("Disabled, see https://github.com/coherence/unity/issues/4631")]
        public async Task Send_Works()
        {
            // Arrange
            var loop = CreateTestTcpTransportLoop();
            var sent = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

            streamMock
                .Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] bytes, int _, int _, CancellationToken ct) =>
                {
                    sent.Add(bytes, ct);
                    return Task.FromResult(true);
                });

            // Act
            var run = loop.Run();

            sendQueue.Enqueue(OctetStreamFromData("Hello"));
            sendQueue.Enqueue(OctetStreamFromData("World"));

            // Assert
            var ok = sent.TryTake(out _, AsyncOperationTimeout);
            Assert.That(ok, Is.True); // Magic + RoomUID

            const int offset = (int)TcpTransportLoop.HEADER_SIZE;

            ok = sent.TryTake(out var data, AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data, offset, data.Length - offset), Is.EqualTo("Hello"));

            ok = sent.TryTake(out data, AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data, offset, data.Length - offset), Is.EqualTo("World"));

            // Cleanup
            cancellationSource.Cancel();
            await run;
        }

        [Test]
        [Ignore("Disabled, see https://github.com/coherence/unity/issues/4631")]
        public async Task Flush_Works()
        {
            // Arrange
            var loop = CreateTestTcpTransportLoop();
            loop.FlushTimeout = TimeSpan.FromSeconds(2);
            var sent = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

            streamMock
                .Setup(m => m.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((byte[] bytes, int _, int _, CancellationToken ct) =>
                {
                    sent.Add(bytes, ct);
                    return Task.FromResult(true);
                });

            // Act
            var run = loop.Run();

            var ok = sent.TryTake(out _, AsyncOperationTimeout);
            Assert.That(ok, Is.True); // Magic + RoomUID

            cancellationSource.Cancel();

            await Task.Yield();

            sendQueue.Enqueue(OctetStreamFromData("EndOf"));
            sendQueue.Enqueue(OctetStreamFromData("Transmission"));

            // Assert
            const int offset = (int)TcpTransportLoop.HEADER_SIZE;

            ok = sent.TryTake(out var data, AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data, offset, data.Length - offset), Is.EqualTo("EndOf"));

            ok = sent.TryTake(out data, AsyncOperationTimeout);
            Assert.That(ok, Is.True);
            Assert.That(Encoding.UTF8.GetString(data, offset, data.Length - offset), Is.EqualTo("Transmission"));

            // Cleanup
            cancellationSource.Cancel();
            await run;
        }

        private async Task<(bool, (byte[], ConnectionException))> TryTakeFromReceiveQueue(TimeSpan timeout)
        {
            var sw = Stopwatch.StartNew();
            (byte[], ConnectionException) result;

            while (!receiveQueue.TryDequeue(out result))
            {
                await Task.Yield();

                if (sw.Elapsed >= timeout)
                {
                    return (false, result);
                }
            }

            return (true, result);
        }

        private static void WritePacket(string data, MemoryStream stream)
        {
            var packet = PacketFromData(data);
            stream.Write(packet.Array!, 0, packet.Count);
        }

        private static ArraySegment<byte> PacketFromData(string data)
        {
            var packet = OctetStreamFromData(data);
            TcpTransportLoop.WriteHeader(packet);
            return packet.Close();
        }

        private static OutOctetStream OctetStreamFromData(string data)
        {
            // Pad out to allow for the two byte header
            var packet = new OutOctetStream(data.Length + 2);
            packet.WriteUint16(0); // Reserve space for the header
            packet.WriteOctets(Encoding.UTF8.GetBytes(data));
            return packet;
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using Brook;
    using Common;
    using Connection;
    using Log;
    using Stats;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Debugging;

    internal class TcpTransportLoop
    {
        public const uint HEADER_SIZE = sizeof(ushort);    // 2 bytes
        private const uint MAX_PACKET_SIZE = ushort.MaxValue / 2; // based on Brisk.MaxMTU
        private const ushort MAGIC_CODE = 1337;
        private const int MAGIC_CODE_SIZE = sizeof(ushort); // 2 bytes
        private const int ROOM_UID_SIZE = sizeof(ulong);    // 8 bytes

        public TimeSpan FlushTimeout { get; set; } = TimeSpan.FromMilliseconds(20);

        private readonly Stream stream;
        private readonly ulong uniqueRoomId;
        private readonly IStats stats;
        private readonly Logger logger;
        private readonly CancellationToken cancellationToken;

        private readonly ConcurrentQueue<(byte[], ConnectionException)> receiveQueue;
        private readonly AsyncQueue<IOutOctetStream> sendQueue;

        private bool runExecuted;

        public TcpTransportLoop(
            Stream stream,
            ulong uniqueRoomId,
            ConcurrentQueue<(byte[], ConnectionException)> receiveQueue,
            AsyncQueue<IOutOctetStream> sendQueue,
            IStats stats,
            Logger logger,
            CancellationToken cancellationToken)
        {
            this.stream = stream;
            this.uniqueRoomId = uniqueRoomId;
            this.receiveQueue = receiveQueue;
            this.sendQueue = sendQueue;
            this.stats = stats;
            this.logger = logger;
            this.cancellationToken = cancellationToken;
        }

        public async Task Run()
        {
            if (runExecuted)
            {
                throw new InvalidOperationException("Run has already been executed.");
            }

            runExecuted = true;

            var receiveLoop = Task.Run(() => RunReceiveLoopAsync());
            var sendLoop = Task.Run(() => RunSendLoopAsync());
            await Task.WhenAll(receiveLoop, sendLoop);
        }

        /// <remarks>This function runs on a ThreadPool thread.</remarks>
        private async Task RunReceiveLoopAsync()
        {
            var headerBuffer = new byte[HEADER_SIZE];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var (headerOk, packetSize) = await ReceivePacketHeader(headerBuffer);
                    if (!headerOk)
                    {
                        return;
                    }

                    var packetOk = await ReceivePacketPayload(packetSize);
                    if (!packetOk)
                    {
                        return;
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                logger.Debug("Receive failure", ("exception", exception));
                receiveQueue.Enqueue((null, new ConnectionException("Receive failure", exception)));
            }
            finally
            {
                logger.Debug("Receive loop finished");
            }
        }

        private async Task<(bool, int)> ReceivePacketHeader(byte[] headerBuffer)
        {
            if (await ReceiveFull(headerBuffer) != headerBuffer.Length)
            {
                return (false, 0);
            }

            int packetSize = BitConverter.ToUInt16(headerBuffer);
            if (packetSize > MAX_PACKET_SIZE)
            {
                logger.Debug($"Unexpectedly big packet", ("size", packetSize));
                receiveQueue.Enqueue((null, new ConnectionException($"Received unexpectedly big packet: {packetSize}B")));
                return (false, 0);
            }

            return (true, packetSize);
        }

        private async Task<bool> ReceivePacketPayload(int packetSize)
        {
            byte[] packet = new byte[packetSize];

            if (await ReceiveFull(packet) != packetSize)
            {
                return false;
            }

            stats.TrackIncomingPacket((uint)packetSize + HEADER_SIZE);
            receiveQueue.Enqueue((packet, null));

            return true;
        }

        /// <remarks>This function runs on a ThreadPool thread.</remarks>
        private async Task RunSendLoopAsync()
        {
            bool flush = true;

            try
            {
                await SendMagicAndRoomUID();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var data = await sendQueue.DequeueAsync(cancellationToken);
                    await SendPacket(data);
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                flush = false;
                logger.Debug("Send failure", ("exception", exception));
                receiveQueue.Enqueue((null, new ConnectionException("Send failure", exception)));
            }
            finally
            {
                if (flush)
                {
                    await FlushSendQueue();
                }

                logger.Debug($"Send loop finished");
            }
        }

        private async Task SendMagicAndRoomUID()
        {
            var headerPacket = CreateMagicAndRoomIDPacket();
            await stream.WriteAsync(headerPacket, 0, headerPacket.Length, cancellationToken);
            stats.TrackOutgoingPacket((uint)headerPacket.Length);
        }

        private byte[] CreateMagicAndRoomIDPacket()
        {
            var packet = new byte[MAGIC_CODE_SIZE + ROOM_UID_SIZE];

            var magic = MAGIC_CODE;

            packet[0] = (byte)(magic);
            packet[1] = (byte)(magic >> 8);

            packet[2] = (byte)(uniqueRoomId);
            packet[3] = (byte)(uniqueRoomId >> 8);
            packet[4] = (byte)(uniqueRoomId >> 16);
            packet[5] = (byte)(uniqueRoomId >> 24);
            packet[6] = (byte)(uniqueRoomId >> 32);
            packet[7] = (byte)(uniqueRoomId >> 40);
            packet[8] = (byte)(uniqueRoomId >> 48);
            packet[9] = (byte)(uniqueRoomId >> 56);

            return packet;
        }

        private async Task SendPacket(IOutOctetStream data, CancellationToken? cancellationToken = null)
        {
            WriteHeader(data);
            var packet = data.Close();

            await stream.WriteAsync(packet.Array, 0, packet.Count, cancellationToken ?? this.cancellationToken);
            stats.TrackOutgoingPacket((uint)packet.Count);

            data.ReturnIfPoolable();
        }

        private async Task FlushSendQueue()
        {
            try
            {
                TimeSpan timeLeft = FlushTimeout;
                var flushTimer = Stopwatch.StartNew();
                var cancellationSource = new CancellationTokenSource(timeLeft);
                var cancellationToken = cancellationSource.Token;

                do
                {
                    IOutOctetStream data = await sendQueue.DequeueAsync(cancellationToken);
                    await SendPacket(data, cancellationToken);

                    timeLeft = FlushTimeout - flushTimer.Elapsed;
                } while (timeLeft > TimeSpan.Zero);
            }
            catch
            {
                // Flush is part of the connection closure
                // so we don't care about exceptions.
            }
        }

        private async Task<int> ReceiveFull(byte[] buffer)
        {
            int totalRead = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, cancellationToken);
                totalRead += read;

                if (totalRead >= buffer.Length)
                {
                    return totalRead;
                }
            }

            return totalRead;
        }

        public static void WriteHeader(IOutOctetStream data)
        {
            var endPosition = data.Position;
            var packetSize = (ushort)(endPosition - HEADER_SIZE);

            DbgAssert.ThatFmt(packetSize < MAX_PACKET_SIZE,
                "Unexpectedly big packet, was: {}, expected: {}",
                packetSize, MAX_PACKET_SIZE);

            data.Seek(0);
            data.WriteUint16(packetSize);

            DbgAssert.ThatFmt(data.Position == HEADER_SIZE,
                "Header size mismatch, was: {}, expected: {}",
                data.Position, HEADER_SIZE);

            data.Seek(endPosition);
        }
    }
}

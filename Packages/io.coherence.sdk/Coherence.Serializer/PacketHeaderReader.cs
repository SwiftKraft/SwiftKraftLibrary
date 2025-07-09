// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using System;
    using Brook;
    using Brook.Octet;
    using SimulationFrame;

    public struct PacketHeaderInfo
    {
        public IInBitStream Stream;
        public SpecialCommand SpecialCommand;
        public SpecialCommandInfo SpecialCommandInfo;
    }

    public struct BasicPacketHeaderInfo
    {
        public byte Flags;
        public AbsoluteSimulationFrame SimulationFrame;
        public SpecialCommand SpecialCommand;
        public SpecialCommandInfo SpecialCommandInfo;

        public override string ToString()
        {
            return $"[basic {SimulationFrame} {SpecialCommand} {SpecialCommandInfo}]";
        }
    }

    public enum SpecialCommandState
    {
        Ignore,
        Reset,
        Normal,
    }

    public struct ClockSpeedFactor
    {
        public int FactorTimesThousand;

        public override string ToString()
        {
            return $"[timefactor {FactorTimesThousand / 1000f:0.###}]";
        }
    }

    public struct SpecialCommandInfo
    {
        public SpecialCommandState State;
        public ClockSpeedFactor ClockSpeedFactor;

        public override string ToString()
        {
            return $"Factor:{ClockSpeedFactor}";
        }
    }

    public static class PacketHeaderReader
    {
        public static BasicPacketHeaderInfo DeserializeBasicHeader(IInOctetStream reader)
        {
            var octet = reader.ReadOctet();
            var specialCommand = SpecialCommand.Normal;
            var specialCommandInfo = new SpecialCommandInfo();
            if ((octet & 0x80) != 0)
            {
                specialCommand = (SpecialCommand)(octet & 0x7f);
                specialCommandInfo = DeserializeSpecialCommand(specialCommand, reader);
                octet = reader.ReadOctet();
            }
            else
            {
                specialCommandInfo.ClockSpeedFactor = new ClockSpeedFactor
                {
                    FactorTimesThousand = 1000,
                };
            }

            var packetType = (PacketType)((octet & 0x60) >> 5);
            if (packetType != PacketType.Bitstreamed)
            {
                throw new Exception($"Only bitstream packet type is supported got {packetType}");
            }

            var simulationFrame = ReadSimulationFrame(reader);

            return new BasicPacketHeaderInfo
            {
                Flags = octet,
                SimulationFrame = simulationFrame,
                SpecialCommand = specialCommand,
                SpecialCommandInfo = specialCommandInfo,
            };
        }

        internal static SpecialCommandInfo DeserializeSpecialCommand(SpecialCommand command, IInOctetStream reader)
        {
            var info = new SpecialCommandInfo();
            switch (command)
            {
                case SpecialCommand.Normal:
                    break;
                case SpecialCommand.Reset:
                    {
                        info.ClockSpeedFactor = ReadClockSpeedFactor(reader);
                        info.State = SpecialCommandState.Reset;
                    }
                    break;
                case SpecialCommand.Synced:
                    {
                        info.ClockSpeedFactor = ReadClockSpeedFactor(reader);
                        info.State = SpecialCommandState.Normal;
                    }
                    break;
                case SpecialCommand.Syncing:
                    break;
            }

            return info;
        }

        private static AbsoluteSimulationFrame ReadSimulationFrame(IInOctetStream stream)
        {
            var raw = stream.ReadUint64();
            return new AbsoluteSimulationFrame
            {
                Frame = (long)raw,
            };
        }

        private static ClockSpeedFactor ReadClockSpeedFactor(IInOctetStream stream)
        {
            var v = stream.ReadUint16();
            return new ClockSpeedFactor
            {
                FactorTimesThousand = v,
            };
        }

        public static PacketHeaderInfo ToPacketHeaderInfo(IInOctetStream octetStream, BasicPacketHeaderInfo basicHeader)
        {
            var flags = basicHeader.Flags & 0x0f;
            var octetCount = octetStream.RemainingOctetCount;
            var isDebugStream = (flags & 0x08) != 0;
            var bitCountInStream = octetCount * 8;
            var rawStream = new InBitStream(octetStream, bitCountInStream);

            IInBitStream stream = isDebugStream ? new DebugInBitStream(rawStream) : rawStream;

            return new PacketHeaderInfo
            {
                Stream = stream,
                SpecialCommand = basicHeader.SpecialCommand,
                SpecialCommandInfo = basicHeader.SpecialCommandInfo,
            };
        }
    }
}

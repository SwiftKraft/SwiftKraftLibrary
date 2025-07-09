// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;

    public enum Mode : byte
    {
        NormalMode = 0x01,
        OobMode = 0x02,
    }

    public readonly struct BriskHeader
    {
        public Mode Mode { get; }

        public BriskHeader(Mode mode)
        {
            Mode = mode;
        }

        public override string ToString()
        {
            return $"{nameof(Mode)}: {Mode}";
        }

        public void Serialize(IOutOctetStream outStream)
        {
            outStream.WriteUint8((byte)Mode);
        }

        public static BriskHeader Deserialize(IInOctetStream stream)
        {
            var mode = stream.ReadUint8();
            return new BriskHeader((Mode)mode);
        }
    }
}

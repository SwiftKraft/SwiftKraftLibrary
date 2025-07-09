// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.ProtocolDef
{
    using Brook;
    using Entities;
    using System.Numerics;
    using Coherence.Common;

    public interface IInProtocolBitStream
    {
        uint ReadBits(int count);
        byte ReadByte();
        sbyte ReadSByte();
        short ReadShort();
        ushort ReadUShort();
        char ReadChar();
        int ReadIntegerRange(int bitCount, int offset);
        uint ReadUIntegerRange(int bitCount, uint offset);
        long ReadLong();
        ulong ReadULong();
        double ReadDouble();
        float ReadFloat(in FloatMeta meta);
        Vector2 ReadVector2(in FloatMeta meta);
        Vector3 ReadVector3(in FloatMeta meta);
        Vector3d ReadVector3d();
        Vector4 ReadVector4(in FloatMeta meta);
        Vector4 ReadColor(in FloatMeta meta);
        Quaternion ReadQuaternion(int bitsPerComponent);
        string ReadShortString();
        bool ReadBool();
        int ReadEnum();
        bool ReadMask();
        uint ReadMaskBits(uint numBits);
        Entity ReadEntity();
        byte[] ReadBytesList();
    }
}

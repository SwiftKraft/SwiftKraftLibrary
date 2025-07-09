// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.ProtocolDef
{
    using Brook;
    using Entities;
    using System.Numerics;
    using Coherence.Common;

    public interface IOutProtocolBitStream
    {
        void WriteBits(uint value, int count);
        void WriteByte(byte value);
        void WriteSByte(sbyte value);
        void WriteShort(short value);
        void WriteUShort(ushort value);
        void WriteChar(char value);
        void WriteIntegerRange(int v, int bitCount, int offset);
        void WriteUIntegerRange(uint v, int bitCount, uint offset);
        void WriteLong(long value);
        void WriteULong(ulong value);
        void WriteDouble(double value);
        void WriteFloat(float value, in FloatMeta meta);
        void WriteVector2(in Vector2 v, in FloatMeta meta);
        void WriteVector3(in Vector3 v, in FloatMeta meta);
        void WriteVector3d(in Vector3d v);
        void WriteVector4(in Vector4 v, in FloatMeta meta);
        void WriteColor(in Vector4 fromUnityColor, in FloatMeta forFixedPoint);
        void WriteQuaternion(in Quaternion q, int bitsPerComponent);
        void WriteShortString(string s);
        void WriteBool(bool b);
        void WriteEnum(int b);
        bool WriteMask(bool b);
        void WriteMaskBits(uint mask, uint numBits);
        void WriteEntity(Entity e);
        void WriteBytesList(byte[] data);
    }
}

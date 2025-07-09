// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using ProtocolDef;
    using Brook;
    using System.Text;
    using Entities;
    using System;
    using System.Numerics;
    using System.Threading;
    using Common;
    using Log;

    public class OutProtocolBitStream : IOutProtocolBitStream
    {
        public const int BYTES_LIST_LENGTH_BITS = 9; // 512 bytes
        public const int BYTES_LIST_MAX_LENGTH = (1 << BYTES_LIST_LENGTH_BITS) - 1;

        public static readonly int BYTE_LIST_OVERHEAD = 2;
        public static readonly int MAX_BYTE_ARRAY_LENGTH = BYTES_LIST_MAX_LENGTH - BYTE_LIST_OVERHEAD;

        public const int SHORT_STRING_LENGTH_BITS = 6; // 64 bytes
        public const int SHORT_STRING_MAX_SIZE = (1 << SHORT_STRING_LENGTH_BITS) - 1;

        public const int ENUM_LENGTH_BITS = 6; // 64 bytes
        public const int ENUM_MAX_VALUE = (1 << ENUM_LENGTH_BITS) - 1;

        public IOutBitStream BitStream { get; private set; }
        private Cram.OutBitStream cramStream;
        private Logger logger;

        [ThreadStatic]
        private static OutProtocolBitStream shared;
        internal static OutProtocolBitStream Shared
        {
            get
            {
                if (shared == null)
                {
                    shared = new OutProtocolBitStream(null, null);
                }
                return shared;
            }
        }

        public OutProtocolBitStream(IOutBitStream bitStream, Logger incoming)
        {
            Reset(bitStream, incoming);
        }

        internal OutProtocolBitStream Reset(IOutBitStream bitStream, Logger incoming)
        {
            BitStream = bitStream;
            cramStream = new Cram.OutBitStream(bitStream);
            logger = incoming;
            return this;
        }

        public void WriteIntegerRange(int v, int bitCount, int offset)
        {
            BitStream.WriteBits((uint)(v - offset), bitCount);
        }

        public void WriteUIntegerRange(uint v, int bitCount, uint offset)
        {
            BitStream.WriteBits((v - offset), bitCount);
        }

        public void WriteDouble(double value)
        {
            cramStream.WriteDouble(value);
        }

        public void WriteFloat(float value, in FloatMeta meta)
        {
            cramStream.WriteFloat(value, meta);
        }

        public void WriteVector2(in Vector2 v, in FloatMeta meta)
        {
            cramStream.WriteVector2(v, meta);
        }

        public void WriteVector3(in Vector3 v, in FloatMeta meta)
        {
            cramStream.WriteVector3(v, meta);
        }

        public void WriteVector3d(in Vector3d v)
        {
            cramStream.WriteDouble(v.x);
            cramStream.WriteDouble(v.y);
            cramStream.WriteDouble(v.z);
        }

        public void WriteVector4(in Vector4 v, in FloatMeta meta)
        {
            cramStream.WriteVector4(v, meta);
        }

        public void WriteColor(in Vector4 v, in FloatMeta meta)
        {
            WriteVector4(v, meta);
        }

        public void WriteQuaternion(in Quaternion q, int bitsPerComponent)
        {
            cramStream.WriteQuaternion(q, bitsPerComponent);
        }

        public void WriteShortString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                BitStream.WriteBits(0, SHORT_STRING_LENGTH_BITS);
                return;
            }

            if (s.Length > SHORT_STRING_MAX_SIZE)
            {
                logger.Error(Error.StringTooLong, ("len", Encoding.UTF8.GetByteCount(s)), ("max", SHORT_STRING_MAX_SIZE));
                s = s.Substring(0, SHORT_STRING_MAX_SIZE);
            }

            // We need additional space in case chars take more than 1 byte.
            // We'll still write only SHORT_STRING_MAX_SIZE bytes.
            Span<byte> octets = stackalloc byte[SHORT_STRING_MAX_SIZE * 4];
            var len = Encoding.UTF8.GetBytes(s, octets);
            len = Math.Min(len, SHORT_STRING_MAX_SIZE);

            BitStream.WriteBits((byte)len, SHORT_STRING_LENGTH_BITS);

            if (len > 0)
            {
                for (int l = 0; l < len; ++l)
                {
                    BitStream.WriteUint8(octets[l]);
                }
            }
        }

        public void WriteBytesList(byte[] data)
        {
            if (data == null)
            {
                BitStream.WriteBits(0, BYTES_LIST_LENGTH_BITS);
                return;
            }

            if (data.Length > BYTES_LIST_MAX_LENGTH)
            {
                throw new ArgumentException($"Array too long. Max size: {BYTES_LIST_MAX_LENGTH} bytes.");
            }

            BitStream.WriteBits((uint)data.Length, BYTES_LIST_LENGTH_BITS);
            for (int i = 0; i < data.Length; i++)
            {
                BitStream.WriteUint8(data[i]);
            }
        }

        public void WriteBits(uint value, int count)
        {
            BitStream.WriteBits(value, count);
        }

        public void WriteByte(byte value)
        {
            BitStream.WriteUint8(value);
        }

        public void WriteSByte(sbyte value)
        {
            BitStream.WriteUint8((byte)value);
        }

        public void WriteShort(short value)
        {
            BitStream.WriteUint16((ushort)value);
        }

        public void WriteUShort(ushort value)
        {
            BitStream.WriteUint16(value);
        }

        public void WriteChar(char value)
        {
            BitStream.WriteUint16(value);
        }

        public void WriteLong(long value)
        {
            BitStream.WriteUint64((ulong)value);
        }

        public void WriteULong(ulong value)
        {
            BitStream.WriteUint64(value);
        }

        public bool WriteMask(bool b)
        {
            BitStream.WriteBits(b ? 1U : 0U, 1);
            return b;
        }

        public void WriteMaskBits(uint mask, uint numBits)
        {
            BitStream.WriteBits(mask, (int)numBits);
        }

        public void WriteBool(bool b)
        {
            BitStream.WriteBits(b ? 1U : 0U, 1);
        }

        public void WriteEnum(int b)
        {
            if (b > ENUM_MAX_VALUE)
            {
                throw new Exception($"Enum too large. Max enum value: {ENUM_MAX_VALUE} bytes.");
            }

            BitStream.WriteBits((uint)b, ENUM_LENGTH_BITS);
        }

        public void WriteEntity(Entity entityID)
        {
            SerializeTools.SerializeEntity(entityID, BitStream);
        }
    }
}

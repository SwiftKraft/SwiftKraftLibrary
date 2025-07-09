// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using Brook;

    public struct SessionID
    {
        public static readonly SessionID None = new SessionID(0);

        public const int Size = sizeof(ushort);
        public readonly ushort Value;

        public SessionID(ushort value)
        {
            Value = value;
        }

        public static SessionID Read(IInOctetStream stream)
        {
            return stream.ReadUint16();
        }

        public static void Write(in SessionID sessionID, IOutOctetStream stream)
        {
            stream.WriteUint16(sessionID.Value);
        }

        public static bool operator == (SessionID l, SessionID r)
        {
            return l.Value == r.Value;
        }

        public static bool operator !=(SessionID l, SessionID r)
        {
            return !(l == r);
        }

        public static implicit operator ushort(SessionID id)
        {
            return id.Value;
        }

        public static implicit operator SessionID(ushort value)
        {
            return new SessionID(value);
        }

        public bool Equals(SessionID other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SessionID other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            var lsb = (byte)Value;
            var msb = (byte)(Value >> 8);

            return $"{lsb:X2}{msb:X2}";
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    using System;

    public readonly struct ClientID : IEquatable<ClientID>, IComparable<ClientID>
    {
        public static readonly ClientID Server = default;

        private readonly uint id;

        public ClientID(uint id)
        {
            this.id = id;
        }

        public static explicit operator uint(ClientID cid) => cid.id;
        public static explicit operator ClientID(uint cid) => new ClientID(cid);

        bool IEquatable<ClientID>.Equals(ClientID other) => Equals(other);
        public bool Equals(in ClientID other) => id == other.id;
        public override bool Equals(object obj) => obj is ClientID other && Equals(other);
        public int CompareTo(ClientID other) => id.CompareTo(other.id);
        public static bool operator ==(in ClientID left, in ClientID right) => left.Equals(right);
        public static bool operator !=(in ClientID left, in ClientID right) => !left.Equals(right);
        public static bool operator > (in ClientID left, in ClientID right) => left.id > right.id;
        public static bool operator >= (in ClientID left, in ClientID right) => left.id >= right.id;
        public static bool operator < (in ClientID left, in ClientID right) => left.id < right.id;
        public static bool operator <= (in ClientID left, in ClientID right) => left.id <= right.id;
        public override int GetHashCode() => (int)id;

        public override string ToString() => id.ToString();

    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;
    using Serializers;

    public struct NameVersion
    {
        public Version Version;
        public string Name;

        public NameVersion(string name, Version version)
        {
            Name = name;
            Version = version;
        }

        public override string ToString()
        {
            return $"{Name} {Version}";
        }

        public void Serialize(IOutOctetStream stream)
        {
            StringSerializer.Serialize(stream, Name);
            Version.Serialize(stream);
        }

        public static NameVersion Deserialize(IInOctetStream stream)
        {
            string name = StringSerializer.Deserialize(stream);
            Version version = Version.Deserialize(stream);

            return new NameVersion(name, version);
        }
    }

    public struct Version
    {
        public ushort Major;
        public ushort Minor;
        public ushort Patch;

        public string Prerelease;

        public Version(ushort major, ushort minor, ushort patch, string prerelease)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = prerelease;
        }


        public override string ToString()
        {
            string s = $"{Major}.{Minor}.{Patch}";

            if (Prerelease.Length != 0)
            {
                s += $"-{Prerelease}";
            }

            return s;
        }

        public void Serialize(IOutOctetStream stream)
        {
            stream.WriteUint16(Major);
            stream.WriteUint16(Minor);
            stream.WriteUint16(Patch);
            StringSerializer.Serialize(stream, Prerelease);
        }

        public static Version Deserialize(IInOctetStream stream)
        {
            ushort major = stream.ReadUint16();
            ushort minor = stream.ReadUint16();
            ushort patch = stream.ReadUint16();

            string prerelease = StringSerializer.Deserialize(stream);

            return new Version(major, minor, patch, prerelease);
        }
    }
}

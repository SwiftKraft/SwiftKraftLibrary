// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Serializers
{
    using Brook;
    using System.Text;

    public static class StringSerializer
    {
        public static void Serialize(IOutOctetStream stream, string value)
        {
            var octets = Encoding.UTF8.GetBytes(value);
            stream.WriteUint8((byte)octets.Length);
            stream.WriteOctets(octets);
        }

        public static string Deserialize(IInOctetStream stream)
        {
            var myNonce = stream.ReadUint8();
            var characters = stream.ReadOctets(myNonce);

            return Encoding.UTF8.GetString(characters);
        }
    }
}

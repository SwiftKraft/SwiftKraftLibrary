// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using System;
    using System.Text;
    using Brook;
    using Coherence.Brisk.Serializers;
    using Coherence.Common;

    public struct ConnectInfo
    {
        public uint ProtocolVersion { get; }
        public string AuthToken { get; }
        public ulong RoomUid { get; }
        public string SchemaId { get; }
        public string RoomSecret { get; private set; }
        public bool IsSimulator { get; }
        public uint Scene { get; }
        public string RSVersion { get; }
        public ushort MTU { get; }
        public CustomPayload UserPayload { get; }  // for connection validation

        public ConnectInfo(
            uint protocolVersion,
            ulong roomUid,
            string schemaId,
            string authToken,
            string roomSecret,
            bool isSimulator,
            uint scene,
            string rsVersion,
            ushort mtu,
            CustomPayload userPayload
        )
        {
            ProtocolVersion = protocolVersion;
            RoomUid = roomUid;
            SchemaId = schemaId;
            AuthToken = authToken;
            RoomSecret = roomSecret;
            IsSimulator = isSimulator;
            Scene = scene;
            RSVersion = rsVersion;
            MTU = mtu;
            UserPayload = userPayload;

            ValidateRoomSecretLength();
        }

        private void ValidateRoomSecretLength()
        {
            if (!string.IsNullOrEmpty(RoomSecret))
            {
                var roomSecretSerializedLength = Encoding.UTF8.GetByteCount(RoomSecret);
                if (roomSecretSerializedLength > byte.MaxValue)
                {
                    throw new ArgumentException($"Invalid {nameof(RoomSecret)} length. " +
                                                $"Max serialized size: {byte.MaxValue}, was: {roomSecretSerializedLength}");
                }
            }
        }

        public override string ToString()
        {
            var token = string.Empty;
            if (AuthToken != null)
            {
                token = AuthToken.Length > 8 ? $"{AuthToken.Substring(0, 8)}..." : "********";
            }

            return $"{nameof(ConnectInfo)}: [" +
                $"Version: {ProtocolVersion}, " +
                $"RoomUID: {RoomUid}, " +
                $"SchemaID: {SchemaId}, " +
                $"AuthToken: {token}, " +
                $"RoomSecret: {!string.IsNullOrEmpty(RoomSecret)}, " +
                $"IsSimulator: {IsSimulator}, " +
                $"MTU: {MTU}, " +
                $"HasPayload: {!UserPayload.IsEmpty}" + // not showing the payload because it could be secret.
                "]";
        }

        public void Serialize(IOutOctetStream stream)
        {
            stream.WriteUint32(ProtocolVersion);
            stream.WriteUint64(RoomUid);
            WriteByteRleString(stream, SchemaId);
            WriteShortRleString(stream, AuthToken);
            WriteByteRleString(stream, RoomSecret);
            WriteBool(stream, IsSimulator);
            stream.WriteUint32(Scene);
            WriteByteRleString(stream, RSVersion);

            if (ProtocolVersion >= ProtocolDef.Version.VersionIncludesConnectInfoMTU)
            {
                stream.WriteUint16(MTU);
            }

            if (ProtocolVersion >= ProtocolDef.Version.VersionIncludesConnectionValidation)
            {
                stream.WriteCustomPayload(UserPayload);
            }
        }

        public static ConnectInfo Deserialize(IInOctetStream stream)
        {
            var version = stream.ReadUint32();
            var roomUid = stream.ReadUint64();
            var schemaId = ReadByteRleString(stream);
            var authToken = ReadShortRleString(stream);
            var roomSecret = ReadByteRleString(stream);
            var isSimulator = ReadBool(stream);
            var scene = stream.ReadUint32();
            var rsVersion = ReadByteRleString(stream);

            var mtu = Brisk.DefaultMTU;

            if (version >= ProtocolDef.Version.VersionIncludesConnectInfoMTU)
            {
                mtu = stream.ReadUint16();
            }

            var payload = CustomPayload.Empty;

            if (version >= ProtocolDef.Version.VersionIncludesConnectionValidation)
            {
                payload = stream.ReadCustomPayload();
            }

            return new ConnectInfo(
                version,
                roomUid,
                schemaId,
                authToken,
                roomSecret,
                isSimulator,
                scene,
                rsVersion,
                mtu,
                payload
            );
        }

        private static string ReadShortRleString(IInOctetStream stream)
        {
            var lengthRaw = stream.ReadOctets(sizeof(short));

            var length = BitConverter.ToInt16(lengthRaw);
            if (length == 0)
            {
                return string.Empty;
            }

            var data = stream.ReadOctets(length);
            return Encoding.UTF8.GetString(data);
        }

        private static void WriteShortRleString(IOutOctetStream stream, string str)
        {
            var authTokenRaw = Encoding.UTF8.GetBytes(str ?? string.Empty);
            var authTokenLengthRaw = BitConverter.GetBytes((short)authTokenRaw.Length);

            stream.WriteOctets(authTokenLengthRaw);
            if (authTokenRaw.Length > 0)
            {
                stream.WriteOctets(authTokenRaw);
            }
        }

        private static string ReadByteRleString(IInOctetStream stream)
        {
            var str = string.Empty;
            var length = stream.ReadOctet();
            if (length > 0)
            {
                var data = stream.ReadOctets(length);
                str = Encoding.UTF8.GetString(data);
            }

            return str;
        }

        private static void WriteByteRleString(IOutOctetStream stream, string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                stream.WriteUint8(0);
            }
            else
            {
                var strBytes = Encoding.UTF8.GetBytes(str);
                stream.WriteUint8((byte)strBytes.Length);
                stream.WriteOctets(strBytes);
            }
        }

        private static bool ReadBool(IInOctetStream stream)
        {
            return stream.ReadUint8() != 0;
        }

        private static void WriteBool(IOutOctetStream stream, bool value)
        {
            stream.WriteUint8(value ? (byte)1 : (byte)0);
        }
    }
}

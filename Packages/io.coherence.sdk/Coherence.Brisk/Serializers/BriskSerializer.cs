// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Serializers
{
    using Brook;
    using Models;
    using System;

    public static class BriskSerializer
    {
        public static void SerializeOobMessage(IOutOctetStream outStream, IOobMessage oobMessage, uint protocolVersion)
        {
            outStream.WriteUint8((byte)oobMessage.Type);
            oobMessage.Serialize(outStream, protocolVersion);
        }

        public static IOobMessage DeserializeOobMessage(IInOctetStream stream, uint protocolVersion)
        {
            var cmd = (OobMessageType)stream.ReadUint8();

            switch (cmd)
            {
                case OobMessageType.ChangeSendFrequencyRequest:
                    return ChangeSendFrequencyRequest.Deserialize(stream, protocolVersion);
                case OobMessageType.ConnectRequest:
                    return ConnectRequest.Deserialize(stream, protocolVersion);
                case OobMessageType.ConnectResponse:
                    return ConnectResponse.Deserialize(stream, protocolVersion);
                case OobMessageType.DisconnectRequest:
                    return DisconnectRequest.Deserialize(stream, protocolVersion);
                case OobMessageType.KeepAlive:
                    return KeepAlive.Deserialize(stream, protocolVersion);
                case OobMessageType.Ack:
                    return Ack.Instance;
                case OobMessageType.ValidateConnectionRequest:
                    return ValidateConnectionRequest.Deserialize(stream, protocolVersion);
                case OobMessageType.ValidateConnectionResponse:
                    return ValidateConnectionResponse.Deserialize(stream, protocolVersion);
                case OobMessageType.KickRequest:
                    return KickRequest.Deserialize(stream, protocolVersion);
                default:
                    throw new Exception($"Unexpected Oob packet type: {cmd}");
            }
        }
    }
}

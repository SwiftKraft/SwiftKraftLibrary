// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;
    using Coherence.Brisk.Serializers;
    using Coherence.Common;
    using Connection;

    public class DisconnectRequest : IOobMessage
    {
        public bool IsReliable { get; set; }
        public OobMessageType Type => OobMessageType.DisconnectRequest;

        public ConnectionCloseReason Reason { get; }
        public CustomPayload HostPayload { get; set; }

        public DisconnectRequest(ConnectionCloseReason reason, CustomPayload hostPayload)
        {
            Reason = reason;
            HostPayload = hostPayload;
        }

        public override string ToString()
        {
            return $"{nameof(DisconnectRequest)}: [ Reason: {Reason}, HasPayload: {!HostPayload.IsEmpty} ]";
        }

        public void Serialize(IOutOctetStream stream, uint protocolVersion)
        {
            stream.WriteUint8((byte)Reason);

            if (protocolVersion >= ProtocolDef.Version.VersionIncludesConnectionKicking)
            {
                stream.WriteCustomPayload(HostPayload);
            }
        }

        public static DisconnectRequest Deserialize(IInOctetStream stream, uint protocolVersion)
        {
            var reason = (ConnectionCloseReason)stream.ReadUint8();

            var payload = CustomPayload.Empty;

            if (protocolVersion >= ProtocolDef.Version.VersionIncludesConnectionKicking)
            {
                payload = stream.ReadCustomPayload();
            }

            return new DisconnectRequest(reason, payload);
        }
    }
}

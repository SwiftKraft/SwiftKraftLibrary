// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;
    using Coherence.Brisk.Serializers;
    using Coherence.Common;
    using Coherence.Connection;

    public class KickRequest : IOobMessage
    {
        public bool IsReliable { get; set; } = true;
        public OobMessageType Type => OobMessageType.KickRequest;

        public ClientID ClientID { get; set; }
        public CustomPayload HostPayload { get; set; }

        public KickRequest(ClientID clientID, CustomPayload hostPayload)
        {
            ClientID = clientID;
            HostPayload = hostPayload;
        }

        public override string ToString()
        {
            return $"{nameof(KickRequest)}: [ Client: {ClientID}, HasPayload: {!HostPayload.IsEmpty} ]";
        }

        public void Serialize(IOutOctetStream stream, uint _)
        {
            stream.WriteUint32((uint)ClientID);
            stream.WriteCustomPayload(HostPayload);
        }

        public static KickRequest Deserialize(IInOctetStream stream, uint _)
        {
            var clientID = (ClientID)stream.ReadUint32();
            var payload = stream.ReadCustomPayload();

            return new KickRequest(clientID, payload);
        }
    }
}

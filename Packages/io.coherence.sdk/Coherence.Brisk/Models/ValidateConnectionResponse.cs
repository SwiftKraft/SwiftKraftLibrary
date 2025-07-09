// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;
    using Coherence.Brisk.Serializers;
    using Coherence.Common;
    using Coherence.Connection;

    public class ValidateConnectionResponse : IOobMessage
    {
        public bool IsReliable { get; set; } = true;
        public OobMessageType Type => OobMessageType.ValidateConnectionResponse;

        public ClientID ClientID { get; set; }
        public bool Accepted { get; set; }
        public CustomPayload HostPayload { get; set; }

        public ValidateConnectionResponse(ClientID clientID, bool accepted, CustomPayload hostPayload)
        {
            ClientID = clientID;
            Accepted = accepted;
            HostPayload = hostPayload;
        }

        public override string ToString()
        {
            return $"{nameof(ValidateConnectionResponse)}: [ Client: {ClientID}, Accepted: {Accepted}, HasPayload: {!HostPayload.IsEmpty} ]";
        }

        public void Serialize(IOutOctetStream stream, uint _)
        {
            stream.WriteUint32((uint)ClientID);
            stream.WriteUint8((byte)(Accepted ? 1 : 0));
            stream.WriteCustomPayload(HostPayload);
        }

        public static ValidateConnectionResponse Deserialize(IInOctetStream stream, uint _)
        {
            var clientID = (ClientID)stream.ReadUint32();
            var accepted = stream.ReadUint8();
            var payload = stream.ReadCustomPayload();

            return new ValidateConnectionResponse(clientID, accepted != 0, payload);
        }
    }
}

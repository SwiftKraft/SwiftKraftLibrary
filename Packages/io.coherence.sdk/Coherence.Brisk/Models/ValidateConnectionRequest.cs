// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;
    using Coherence.Brisk.Serializers;
    using Coherence.Common;
    using Coherence.Connection;

    public class ValidateConnectionRequest : IOobMessage
    {
        public bool IsReliable { get; set; } = true;
        public OobMessageType Type => OobMessageType.ValidateConnectionRequest;

        public ClientID ClientID { get; set; }
        public CustomPayload UserPayload { get; set; }

        public ValidateConnectionRequest(ClientID clientID, CustomPayload userPayload)
        {
            ClientID = clientID;
            UserPayload = userPayload;
        }

        public override string ToString()
        {
            return $"{nameof(ValidateConnectionRequest)}: [ Client: {ClientID}, HasPayload: {!UserPayload.IsEmpty} ]";
        }

        public void Serialize(IOutOctetStream stream, uint _)
        {
            stream.WriteUint32((uint)ClientID);
            stream.WriteCustomPayload(UserPayload);
        }

        public static ValidateConnectionRequest Deserialize(IInOctetStream stream, uint _)
        {
            var clientID = (ClientID)stream.ReadUint32();
            var payload = stream.ReadCustomPayload();

            return new ValidateConnectionRequest(clientID, payload);
        }
    }
}

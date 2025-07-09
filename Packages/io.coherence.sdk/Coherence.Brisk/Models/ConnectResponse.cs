// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;
    using Coherence.Brisk.Serializers;
    using Common;
    using Connection;

    public class ConnectResponse : IOobMessage
    {
        public bool IsReliable { get; set; }
        public OobMessageType Type => OobMessageType.ConnectResponse;

        public ClientID ClientID { get; }
        public byte SendFrequency { get; }
        public ulong SimulationFrame { get; }
        public ushort MTU { get; }
        public CustomPayload HostPayload { get; }

        public ConnectResponse(ClientID clientID, byte sendFrequency, ulong simulationFrame, ushort mtu, CustomPayload hostPayload)
        {
            ClientID = clientID;
            SendFrequency = sendFrequency;
            SimulationFrame = simulationFrame;
            MTU = mtu;
            HostPayload = hostPayload;
        }

        public override string ToString()
        {
            return $"{nameof(ConnectResponse)}: [ ClientID: {ClientID}, SendFreq: {SendFrequency}, " +
                $"SimFrame: {SimulationFrame}, HasPayload: {!HostPayload.IsEmpty} ]";
        }

        public void Serialize(IOutOctetStream stream, uint protocolVersion)
        {
            stream.WriteUint32((uint)ClientID);
            stream.WriteUint8(SendFrequency);
            stream.WriteUint64(SimulationFrame);

            if (protocolVersion >= ProtocolDef.Version.VersionIncludesConnectInfoMTU)
            {
                stream.WriteUint16(MTU);
            }

            if (protocolVersion >= ProtocolDef.Version.VersionIncludesConnectResponseHostPayload)
            {
                stream.WriteCustomPayload(HostPayload);
            }
        }

        public static ConnectResponse Deserialize(IInOctetStream stream, uint protocolVersion)
        {
            ClientID clientID = (ClientID)stream.ReadUint32();
            byte updateFrequency = stream.ReadUint8();
            ulong simulationFrame = stream.ReadUint64();

            var mtu = Brisk.DefaultMTU;

            if (protocolVersion >= ProtocolDef.Version.VersionIncludesConnectInfoMTU)
            {
                mtu = stream.ReadUint16();
            }

            var payload = CustomPayload.Empty;

            if (protocolVersion >= ProtocolDef.Version.VersionIncludesConnectResponseHostPayload)
            {
                payload = stream.ReadCustomPayload();
            }

            return new ConnectResponse(clientID, updateFrequency, simulationFrame, mtu, payload);
        }
    }
}

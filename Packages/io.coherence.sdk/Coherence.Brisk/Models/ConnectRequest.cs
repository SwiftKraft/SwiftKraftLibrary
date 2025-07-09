// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;

    public class ConnectRequest : IOobMessage
    {
        public bool IsReliable { get; set; }
        public OobMessageType Type => OobMessageType.ConnectRequest;

        public ConnectInfo Info { get; }

        public ConnectRequest(ConnectInfo info)
        {
            Info = info;
        }

        public override string ToString()
        {
            return $"{nameof(ConnectRequest)}: [ Info: {Info} ]";
        }

        public void Serialize(IOutOctetStream stream, uint _)
        {
            Info.Serialize(stream);
        }

        public static ConnectRequest Deserialize(IInOctetStream stream, uint _)
        {
            var info = ConnectInfo.Deserialize(stream);
            return new ConnectRequest(info);
        }
    }
}

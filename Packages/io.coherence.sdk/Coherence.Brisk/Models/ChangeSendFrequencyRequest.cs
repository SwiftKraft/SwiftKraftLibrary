// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;

    public class ChangeSendFrequencyRequest : IOobMessage
    {
        public bool IsReliable { get; set; } = true;
        public OobMessageType Type => OobMessageType.ChangeSendFrequencyRequest;

        public byte sendFrequency { get; }

        public ChangeSendFrequencyRequest(byte frequency)
        {
            sendFrequency = frequency;
        }

        public override string ToString()
        {
            return $"{nameof(ChangeSendFrequencyRequest)}: [ SendFrequency: {sendFrequency} ]";
        }

        public void Serialize(IOutOctetStream stream, uint _)
        {
            stream.WriteUint8(sendFrequency);
        }

        public static ChangeSendFrequencyRequest Deserialize(IInOctetStream stream, uint _)
        {
            var frequency = stream.ReadUint8();
            return new ChangeSendFrequencyRequest(frequency);
        }
    }
}

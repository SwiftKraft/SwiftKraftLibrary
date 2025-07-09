// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using Brook;

    /// <summary>
    /// Deserializes commands from the Coherence protocol stream.
    /// </summary>
    public static class DeserializeCommands
    {
        public static bool DeserializeCommand(IInBitStream stream, out MessageType messageType)
        {
            // ProtocolVersion < VersionIncludesEndOfPacketMarker
            if (stream.RemainingBits() < Serialize.NUM_BITS_FOR_MESSAGE_TYPE)
            {
                messageType = default;
                return false;
            }

            // ProtocolVersion >= VersionIncludesEndOfPacketMarker
            byte commandValue = stream.ReadUint8();
            if ((MessageType)commandValue == MessageType.EndOfMessages)
            {
                messageType = default;
                return false;
            }

            messageType = (MessageType)commandValue;
            return true;
        }
    }
}

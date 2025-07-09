// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using Common;
    using Log;
    using Octet;

    using Logger = Log.Logger;

    public struct OutPacket
    {
        public readonly IOutOctetStream Stream;
        public readonly SequenceId SequenceId;
        public readonly bool IsReliable;
        public readonly bool IsOob;

        public OutPacket(IOutOctetStream stream, SequenceId sequenceId, bool isReliable, bool isOob, Logger logger)
        {
            if (stream == null)
            {
                // Trying to catch https://app.zenhub.com/workspaces/engine-group-5fb3b64dabadec002057e6f2/issues/gh/coherence/engine/2263
                // where there's a null reference possibly in the OutPacket stream.
                logger.Warning(Warning.OutPacketStreamNull);
                stream = new OutOctetStream(1280); // just need something reasonable here.
            }

            Stream = stream;
            SequenceId = sequenceId;
            IsReliable = isReliable;
            IsOob = isOob;
        }

        public OutPacket WithStream(IOutOctetStream stream, Logger logger)
        {
            return new OutPacket(stream, SequenceId, IsReliable, IsOob, logger);
        }
    }
}

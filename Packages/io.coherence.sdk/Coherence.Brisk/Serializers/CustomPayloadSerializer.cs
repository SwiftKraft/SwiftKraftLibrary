// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Serializers
{
    using System;
    using Coherence.Brook;
    using Coherence.Common;

    internal static class CustomPayloadSerializer
    {
        internal static void WriteCustomPayload(this IOutOctetStream stream, CustomPayload payload)
        {
            if (payload.Bytes == null || payload.Bytes.Length == 0)
            {
                stream.WriteUint16(0);
            }
            else
            {
                if (payload.Bytes.Length > CustomPayload.MaxCustomPayloadLen)
                {
                    throw new Exception($"payload length is too long, got: {payload.Bytes.Length}, max: {CustomPayload.MaxCustomPayloadLen}");
                }

                stream.WriteUint16((ushort)payload.Bytes.Length);
                stream.WriteOctets(payload.Bytes);
            }
        }

        internal static CustomPayload ReadCustomPayload(this IInOctetStream stream)
        {
            byte[] payload = null;

            var payloadLen = stream.ReadUint16();
            if (payloadLen > 0)
            {
                payload = stream.ReadOctets(payloadLen).ToArray();
            }

            return new CustomPayload(payload);
        }
    }
}

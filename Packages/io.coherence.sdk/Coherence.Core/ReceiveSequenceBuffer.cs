// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System.Collections.Generic;
    using Brook;
    using ProtocolDef;

    /// <summary>
    /// ReceiveSequenceBuffer contains a list of ordered messages received or pending.
    /// It works like a ring buffer, and only accepts messages in specific range
    /// of the sequence.
    /// </summary>
    internal class ReceiveSequenceBuffer : SequenceBuffer<IEntityMessage>
    {
        // The next message id in the sequence.
        private MessageID nextID = new MessageID(0);

        public ReceiveSequenceBuffer(int size) : base(size)
        {
        }

        public bool InsertMessage(MessageID id, IEntityMessage message)
        {
            if (IsOldMessage(id))
            {
                return false;
            }

            Insert(id, message);

            return true;
        }

        public void FlushMessages(List<IEntityMessage> messages)
        {
            messages.Clear();

            var message = Find(nextID);
            while (message is not null && messages.Count < Size)
            {
                messages.Add(message);
                Remove(nextID);
                nextID = nextID.Next();
                message = Find(nextID);
            }
        }

        public void Clear()
        {
            nextID = new MessageID(0);
            ClearBuffer();
        }

        // Checks if the sequence id is out of the current receive window,
        // if so it means that is an old resent message.
        private bool IsOldMessage(MessageID id)
        {
            var maxId = nextID.Advance(Size-1);

            return id.Distance(maxId) >= Size;
        }

        public bool IsSequenceReady() => Find(nextID) != null;
    }
}

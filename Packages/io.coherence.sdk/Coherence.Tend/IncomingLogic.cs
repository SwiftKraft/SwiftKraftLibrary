// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tend.Client
{
    using System;
    using Brook;
    using Models;
    using Log;

    public class IncomingLogic : IIncomingLogic
    {
        public SequenceId LastReceivedToUs => lastReceivedToUs;
        public ReceiveMask ReceiveMask => new ReceiveMask(receiveMask);

        private SequenceId lastReceivedToUs = SequenceId.Max;
        private uint receiveMask;

        private Logger logger;

        public IncomingLogic(Logger logger)
        {
            this.logger = logger.With<IncomingLogic>();
        }

        public bool ReceivedToUs(SequenceId nextId)
        {
            if (!lastReceivedToUs.IsValidSuccessor(nextId))
            {
                return false;
            }

            int distance = lastReceivedToUs.Distance(nextId);
            if (distance > ReceiveMask.Range)
            {
                throw new Exception("too big gap in sequence." + distance);
            }

            logger.Trace("received", ("last", lastReceivedToUs), ("next", nextId), ("distance", distance));

            for (int i = 0; i < distance - 1; ++i)
            {
                Append(false);
            }

            Append(true);
            lastReceivedToUs = nextId;

            return true;
        }

        private void Append(bool bit)
        {
            receiveMask <<= 1;
            receiveMask |= bit ? 0x1 : (uint)0x0;
        }
    }
}

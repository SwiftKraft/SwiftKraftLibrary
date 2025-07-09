namespace Coherence.Tend
{
    using Coherence.Brook;
    using Coherence.Tend.Models;

    public interface IOutgoingLogic
    {
        int Count { get; }
        bool CanIncrementOutgoingSequence { get; }
        SequenceId LastReceivedByRemoteSequenceId { get; }
        SequenceId OutgoingSequenceId { get; set; }

        bool ReceivedByRemote(SequenceId receivedByRemoteId, ReceiveMask receivedByRemoteMask);
        SequenceId IncreaseOutgoingSequenceId();
        DeliveryInfo Dequeue();
    }
}

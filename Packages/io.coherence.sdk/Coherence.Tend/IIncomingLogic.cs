namespace Coherence.Tend
{
    using Coherence.Brook;
    using Coherence.Tend.Models;

    public interface IIncomingLogic
    {
        SequenceId LastReceivedToUs { get; }
        ReceiveMask ReceiveMask { get; }

        bool ReceivedToUs(SequenceId nextId);
    }
}

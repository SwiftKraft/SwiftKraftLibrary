// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    public interface ICoherenceInput
    {
        /// <inheritdoc cref="CoherenceInput.IsServerSimulated"/>
        bool IsServerSimulated { get; }

        /// <inheritdoc cref="CoherenceInput.IsReadyToProcessInputs"/>
        bool IsReadyToProcessInputs { get; }

        /// <inheritdoc cref="CoherenceInput.IsProducer"/>
        bool IsProducer { get; }

        /// <inheritdoc cref="CoherenceInput.ProcessingEnabled"/>
        bool ProcessingEnabled { get; }

        /// <inheritdoc cref="CoherenceInput.Buffer"/>
        IInputBuffer Buffer { get; }

        /// <inheritdoc cref="CoherenceInput.BufferSize"/>
        int BufferSize { get; }

        /// <inheritdoc cref="CoherenceInput.Delay"/>
        int Delay { get; set; }

        /// <inheritdoc cref="CoherenceInput.CurrentSimulationFrame"/>
        long CurrentSimulationFrame { get; }

        /// <inheritdoc cref="CoherenceInput.LastFrame"/>
        long LastFrame { get; }

        /// <inheritdoc cref="CoherenceInput.LastSentFrame"/>
        long LastSentFrame { get; }

        /// <inheritdoc cref="CoherenceInput.LastAcknowledgedFrame"/>
        long LastAcknowledgedFrame { get; }

        /// <inheritdoc cref="CoherenceInput.LastReceivedFrame"/>
        long LastReceivedFrame { get; }

        /// <inheritdoc cref="CoherenceInput.MispredictionFrame"/>
        long? MispredictionFrame { get; }

        /// <inheritdoc cref="CoherenceInput.ShouldPause"/>
        bool ShouldPause(long commonReceivedFrame);
    }
}

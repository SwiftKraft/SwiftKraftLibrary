// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Bindings
{
    using SimulationFrame;

    // Used with BakeConditional attribute to generate a no-op binding for a component that is not baked.
    // The reason for it is, if the baked binding code is not found, we currently default to reflection-based.
    // The lookup is made in CoherenceSync.BakeBindings->BakeValueBinding method.
    // Potentially to be removed with coherence/unity#6958.
    public class NoOpBinding : Binding
    {
        public override string CoherenceComponentName { get; }

        public NoOpBinding(string coherenceComponentName)
        {
            CoherenceComponentName = coherenceComponentName;
        }

        public override void IsDirty(AbsoluteSimulationFrame simulationFrame,
            out bool dirty,
            out bool justStopped)
        {
            dirty = false;
            justStopped = false;
        }

        public override void MarkAsReadyToSend()
        {

        }

        internal override bool Activate() => true;
    }
}

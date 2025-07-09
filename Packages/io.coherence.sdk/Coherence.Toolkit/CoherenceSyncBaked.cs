// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Bindings;
    using Coherence.Entities;
    using Coherence.ProtocolDef;
    using Coherence.SimulationFrame;
    using System;
    using System.Collections.Generic;
    using Logger = Coherence.Log.Logger;

    public abstract class CoherenceSyncBaked : IDisposable
    {
        public abstract void Initialize(Entity entityId, CoherenceBridge bridge, IClient client, CoherenceInput input, Logger logger);
        public abstract Binding BakeValueBinding(Binding valueBinding);
        public abstract void BakeCommandBinding(CommandBinding commandBinding, CommandsHandler commandsHandler);
        public virtual void ReceiveCommand(IEntityCommand command) { }
        public abstract void CreateEntity(bool usesLodsAtRuntime, string archetypeName, AbsoluteSimulationFrame simFrame, List<ICoherenceComponentData> components);
        public virtual void SendInputState() { }
        public abstract void Dispose();
        // REMINDER! When adding a method to this class, make sure to implement it in both
        // the regular baked sync script *and* in the fallback "safe" version.
    }
}

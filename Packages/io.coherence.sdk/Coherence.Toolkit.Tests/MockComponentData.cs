namespace Coherence.Toolkit.Tests
{
    using Coherence;
    using Entities;
    using ProtocolDef;
    using SimulationFrame;
    using Toolkit;
    using Bindings;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class MockComponentData : ICoherenceComponentData
    {
        private uint componentType = 0;

        public uint FieldsMask { get; set; }
        public uint StoppedMask { get; set; }
        public uint InitialFieldsMask() => 0;
        public bool HasFields() => true;
        public bool HasRefFields() => false;
        public HashSet<Entity> GetEntityRefs() => default;
        public IEntityMapper.Error MapToAbsolute(IEntityMapper mapper) => IEntityMapper.Error.None;
        public IEntityMapper.Error MapToRelative(IEntityMapper mapper) => IEntityMapper.Error.None;
        public ICoherenceComponentData Clone() => this;

        public int PriorityLevel() => 0;

        public MockComponentData(uint componentType)
        {
            this.componentType = componentType;
        }

        public int GetFieldCount() => 1;

        public long[] GetSimulationFrames() { return default; }

        public uint GetComponentType()
        {
            return componentType;
        }

        public void ResetFrame(AbsoluteSimulationFrame frame)
        {

        }

        public void SetSimulationFrame(AbsoluteSimulationFrame frame)
        {
            throw new System.NotImplementedException();
        }

        public AbsoluteSimulationFrame GetSimulationFrame()
        {
            throw new System.NotImplementedException();
        }

        public AbsoluteSimulationFrame? GetMinSimulationFrame()
        {
            AbsoluteSimulationFrame? min = null;


            return min;
        }

        public ICoherenceComponentData MergeWith(ICoherenceComponentData data)
        {
            return this;
        }

        public uint DiffWith(ICoherenceComponentData data)
        {
            return 0;
        }

        public int GetComponentOrder()
        {
            return 0;
        }

        public bool IsSendOrdered()
        {
            return false;
        }

        public void ResetByteArrays(ICoherenceComponentData other, uint mask)
        {
        }

        public uint ReplaceReferences(Entity fromEntity, Entity toEntity)
        {
            return 0;
        }
    }
}

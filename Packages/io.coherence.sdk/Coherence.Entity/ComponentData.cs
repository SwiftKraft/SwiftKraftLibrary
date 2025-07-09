// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    using System.Collections.Generic;
    using Coherence.SimulationFrame;

    public interface ICoherenceComponentData
    {
        uint GetComponentType();
        int PriorityLevel();
        AbsoluteSimulationFrame? GetMinSimulationFrame();
        ICoherenceComponentData MergeWith(ICoherenceComponentData data);
        uint DiffWith(ICoherenceComponentData data);
        int GetComponentOrder();
        bool IsSendOrdered();
        uint FieldsMask { get; set; }
        uint StoppedMask { get; set; }
        uint InitialFieldsMask();
        bool HasFields();
        bool HasRefFields();
        HashSet<Entity> GetEntityRefs();
        IEntityMapper.Error MapToAbsolute(IEntityMapper mapper);
        IEntityMapper.Error MapToRelative(IEntityMapper mapper);
        ICoherenceComponentData Clone();
        int GetFieldCount();
        long[] GetSimulationFrames();

        /// <summary>
        /// Resets the current simulation frame for all fields of this component.
        /// Used by the TC which needs to create components and set their frames immediately for all fields.
        /// </summary>
        void ResetFrame(AbsoluteSimulationFrame frame);

        /// <summary>
        /// Replaces all references to 'fromEntity' with 'toEntity' in this component.
        /// </summary>
        /// <returns>The field mask of references that were changed.</returns>
        uint ReplaceReferences(Entity fromEntity, Entity toEntity);
    }

    public static class ICoherenceComponentDataEx
    {
        public static string ToStringEx(this ICoherenceComponentData[] comps)
        {
            if (comps == null)
            {
                return "None";
            }

            return string.Join<ICoherenceComponentData>(", ", comps);
        }
    }
}

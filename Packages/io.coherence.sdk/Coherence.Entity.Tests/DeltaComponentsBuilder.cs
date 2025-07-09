// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entity.Tests
{
    using Coherence.SimulationFrame;
    using Entities;
    using Generated;
    using UnityEngine;

    public class DeltaComponentsBuilder
    {
        private DeltaComponents components = DeltaComponents.New();

        public DeltaComponents Build()
        {
            return components;
        }

        public DeltaComponentsBuilder AddConnectedEntity(Entity parent)
        {
            var connectedEntity = new ConnectedEntity
            {
                value = parent
            };
            return AddComponent(connectedEntity);
        }

        public DeltaComponentsBuilder AddSimFramesComponent(
            int? intValue = null, AbsoluteSimulationFrame intSimulationFrame = default,
            float? float0Value = null,
            float? float1Value = null, AbsoluteSimulationFrame floatSimulationFrame = default)
        {
            uint mask = 0;
            if (intValue != null)
            {
                mask |= SimFramesComponent.simFrameIntValueMask;
            }
            if (float0Value != null)
            {
                mask |= SimFramesComponent.floatValueMask;
            }
            if (float1Value != null)
            {
                mask |= SimFramesComponent.simFrameFloatValueMask;
            }

            var component = new SimFramesComponent()
            {
                simFrameIntValue = intValue ?? 0,
                simFrameIntValueSimulationFrame = intSimulationFrame,
                floatValue = float0Value ?? 0,
                simFrameFloatValue = float1Value ?? 0,
                simFrameFloatValueSimulationFrame = floatSimulationFrame,
                FieldsMask = mask
            };
            components.UpdateComponent(ComponentChange.New(component));

            return this;
        }

        public DeltaComponentsBuilder AddGlobal()
        {
            var global = new Global();
            return AddComponent(global);
        }

        public DeltaComponentsBuilder RemoveGlobal()
        {
            components.RemoveComponent(Definition.InternalGlobal);

            return this;
        }

        public DeltaComponentsBuilder AddPosition(Vector3 position = default)
        {
            var worldPosition = new WorldPosition
            {
                value = position
            };
            return AddComponent(worldPosition);
        }

        private DeltaComponentsBuilder AddComponent(ICoherenceComponentData component)
        {
            component.FieldsMask = component.InitialFieldsMask();
            components.UpdateComponent(ComponentChange.New(component));

            return this;
        }
    }
}

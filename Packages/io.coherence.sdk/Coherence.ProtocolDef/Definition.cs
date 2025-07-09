// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.ProtocolDef
{
    using System.Collections.Generic;
    using System.Linq;
    using Brook;
    using SimulationFrame;
    using Connection;
    using Entities;
    using Log;

    public class EntityArchetypeLOD
    {
        public uint Level;
        public float Distance;
        public Dictionary<uint, uint> ComponentReplacement;
        public uint[] ComponentsExcluded;

        public uint[] RemovedComponentsAtLevel()
        {
            return ComponentsExcluded;
        }

        public bool IsExcludedComponentType(uint comp)
        {
            return ComponentsExcluded.Contains(comp);
        }

        public bool SpecializedComponentType(uint baseComponentType, out uint mapped)
        {
            return ComponentReplacement.TryGetValue(baseComponentType, out mapped);
        }
    }

    public struct EntityArchetype
    {
        public EntityArchetypeLOD[] LODs;

        public bool LODForDistance(double distance, out EntityArchetypeLOD lod)
        {
            for (int i = LODs.Length - 1; i >= 0; i--)
            {
                lod = LODs[i];

                if (distance >= (double)lod.Distance)
                {
                    return true;
                }
            }

            lod = null;

            return false;
        }
    }

    public interface IBaseRequest
    {
        ChannelID ChannelID { get; set; }
    }

    public interface IEntityMessage : IBaseRequest
    {
        Entity Entity { get; set; }
        MessageTarget Routing { get; set; }
        uint Sender { get; set; }
        uint GetComponentType();
        IEntityMessage Clone();
        IEntityMapper.Error MapToAbsolute(IEntityMapper mapper, Logger logger);
        IEntityMapper.Error MapToRelative(IEntityMapper mapper, Logger logger);
        HashSet<Entity> GetEntityRefs();
        void NullEntityRefs(Entity entity);
    }

    public interface IEntityCommand : IEntityMessage
    {

    }

    public interface IEntityInput : IEntityMessage
    {
        long Frame { get; }
    }

    public struct ErrorMessage : IEntityMessage
    {
        public Entity Entity { get; set; }
        public ChannelID ChannelID { get; set; }
        public MessageTarget Routing { get; set; }
        public uint Sender { get; set; }
        public uint GetComponentType() => 0;
        public IEntityMessage Clone()
        {
            return new ErrorMessage()
            {
                Entity = Entity,
                Routing = Routing,
                Sender = Sender,
            };
        }
        public IEntityMapper.Error MapToAbsolute(IEntityMapper mapper, Logger logger) => IEntityMapper.Error.None;
        public IEntityMapper.Error MapToRelative(IEntityMapper mapper, Logger logger) => IEntityMapper.Error.None;
        public HashSet<Entity> GetEntityRefs() => default;
        public void NullEntityRefs(Entity entity) { }
    }

    public struct InputData : IEntityInput
    {
        public Entity Entity { get; set; }
        public ChannelID ChannelID { get; set; }
        public MessageTarget Routing { get; set; }
        public uint Sender { get; set; }
        public long Frame { get; set; }
        public IEntityInput Input;
        public uint GetComponentType() => Input.GetComponentType();
        public IEntityMessage Clone()
        {
            return new InputData()
            {
                Entity = Entity,
                Routing = Routing,
                Sender = Sender,
                Frame = Frame,
                Input = (IEntityInput)Input.Clone(),
            };
        }
        public IEntityMapper.Error MapToAbsolute(IEntityMapper mapper, Logger logger)
        {
            var err = mapper.MapToAbsoluteEntity(Entity, false, out var absoluteEntity);
            if (err != IEntityMapper.Error.None)
            {
                return err;
            }

            Entity = absoluteEntity;
            return Input.MapToAbsolute(mapper, logger);
        }
        public IEntityMapper.Error MapToRelative(IEntityMapper mapper, Logger logger)
        {
            var err = mapper.MapToRelativeEntity(Entity, false, out var relativeEntity);
            if (err != IEntityMapper.Error.None)
            {
                return err;
            }

            Entity = relativeEntity;
            return Input.MapToRelative(mapper, logger);
        }
        public HashSet<Entity> GetEntityRefs() => default;
        public void NullEntityRefs(Entity entity) { }
    }

    public interface ISchemaSpecificComponentDeserialize
    {
        ICoherenceComponentData ReadComponentUpdate(uint componentType, AbsoluteSimulationFrame referenceSimulationFrame, IInBitStream bitStreamm, Logger logger);
        IEntityCommand[] ReadCommands(IInBitStream bitStream, Logger logger);
        IEntityInput[] ReadInputs(IInBitStream bitStream, Logger logger);
        IEntityCommand ReadCommand(IInBitStream bitStream, Logger logger);
    }

    public interface ISchemaSpecificComponentSerialize
    {
        uint WriteComponentUpdate(ICoherenceComponentData data, uint serializeAs, bool isRefSimFrameValid, AbsoluteSimulationFrame referenceSimulationFrame, IOutProtocolBitStream protocolStream, Logger logger);
        void WriteCommand(IEntityCommand data, uint commandType, IOutProtocolBitStream bitStream, Logger logger);
        void WriteInput(IEntityInput data, uint inputType, IOutProtocolBitStream bitStream, Logger logger);
    }

    public interface IAuthorityManagement
    {
        bool TryGetAuthorityRequestCommand(IEntityCommand entityCommand, out ClientID requester, out AuthorityType authType);
        bool TryGetAuthorityTransferCommand(IEntityCommand entityCommand, out ClientID newAuthority, out bool transferAccepted, out AuthorityType authType);

        IEntityCommand CreateAuthorityRequest(Entity entity, ClientID requester, AuthorityType authorityType);
        IEntityCommand CreateAuthorityTransfer(Entity entity, ClientID newAuthority, bool accepted, AuthorityType authorityType);
        IEntityCommand CreateAdoptOrphanCommand();
    }

    public interface IBuiltInComponentAccess
    {
        ICoherenceComponentData GenerateCoherenceUUIDData(string UUID, AbsoluteSimulationFrame simFrame);
        ICoherenceComponentData CreateGlobalComponent();
        ICoherenceComponentData GenerateGlobalQueryComponent();

        string ExtractCoherenceUUID(ICoherenceComponentData data);
        string ExtractCoherenceTag(ICoherenceComponentData data);

        bool IsConnectedEntity(ICoherenceComponentData data);
        Entity ExtractConnectedEntityID(ICoherenceComponentData data);

        bool TryGetSceneIndexChangedCommand(IEntityCommand entityCommand, out int sceneIndex);
        IEntityCommand CreateSceneIndexChangedCommand(Entity entity, int sceneIndex);
    }

    public interface IDefinition : ISchemaSpecificComponentDeserialize, ISchemaSpecificComponentSerialize, IAuthorityManagement, IBuiltInComponentAccess, IComponentInfo { }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using System.Collections.Generic;
    using Entities;
    using Log;
    using ProtocolDef;

    public class TestMessage : IEntityMessage
    {
        public Entity Entity { get; set; }
        public ChannelID ChannelID { get; set; }
        public MessageTarget Routing { get; set; }
        public uint Sender { get; set; }

        private readonly uint componentType;

        public TestMessage(
            Entity entity = default,
            uint componentType = 4096,
            MessageTarget routing = MessageTarget.All,
            uint sender = 0)
        {
            this.componentType = componentType;
            Entity = entity;
            Routing = routing;
            Sender = sender;
        }

        public uint GetComponentType()
        {
            return componentType;
        }

        public IEntityMessage Clone()
        {
            throw new System.NotImplementedException();
        }

        public IEntityMapper.Error MapToAbsolute(IEntityMapper mapper, Logger logger)
        {
            return IEntityMapper.Error.None;
        }

        public IEntityMapper.Error MapToRelative(IEntityMapper mapper, Logger logger)
        {
            return IEntityMapper.Error.None;
        }

        public HashSet<Entity> GetEntityRefs() => default;
        public void NullEntityRefs(Entity entity) { }
    }
}

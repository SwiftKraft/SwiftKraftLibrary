// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using System.Collections;
    using System.Text;
    using Entities;

    public class SerializerException : Exception
    {
        public string Section { get; private set; }
        public Entity EntityId { get; private set; }
        public uint ComponentId { get; private set; }

        public override string Message
        {
            get
            {
                return messageOverride ?? base.Message;
            }
        }

        private string messageOverride = null;

        public SerializerException(string section, Entity entityId, uint componentId, Exception inner)
            : base(null, inner)
        {
            Section = section;
            EntityId = entityId;
            ComponentId = componentId;
        }

        public string ToStringEx(string entityName)
        {
            messageOverride = BuildMessage(entityName);
            var str = this.ToString();
            messageOverride = null;

            return str;
        }

        private string BuildMessage(string entityName)
        {
            var builder = new StringBuilder(256);

            builder.Append($"[PacketSection={Section}");
            if (EntityId.IsValid)
            {
                builder.Append($", Entity=({EntityId}, '{entityName ?? "?"}')");
            }
            if (ComponentId != UInt32.MaxValue)
            {
                builder.Append($", Component={ComponentId}");
            }
            builder.Append("]");

            return builder.ToString();
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Coherence.Brook;
    using Coherence.Entities;
    using Coherence.ProtocolDef;
    using Coherence.SimulationFrame;

    /// <summary>
    /// A channel for incoming entity changes/commands/inputs. It's responsible for
    /// deserializing and buffering incoming changes.
    /// </summary>
    internal interface IInNetworkChannel
    {
        event Action<List<IncomingEntityUpdate>> OnEntityUpdate;
        event Action<IEntityCommand, MessageTarget, Entity> OnCommand;
        event Action<IEntityInput, long, Entity> OnInput;

        /// <summary>
        /// Deserializes the bitstream and performs the various messages contained until EndOfMessages is encountered.
        /// </summary>
        /// <returns>True if the stream contained any world update messages, otherwise false.</returns>
        bool Deserialize(IInBitStream stream, AbsoluteSimulationFrame packetSimulationFrame, Vector3 floatingOriginDelta);

        /// <summary>
        /// Returns a graph of all entity references which are ready to be pushed out of the buffer.
        /// Includes entities which reference no other entities too.
        /// </summary>
        List<RefsInfo> GetRefsInfos();

        /// <summary>
        /// Given the set of resolvable entities, pushes buffered changes, commands and inputs to events.
        /// </summary>
        void FlushBuffer(IReadOnlyCollection<Entity> resolvableEntities);

        void Clear();
    }

}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using Entities;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using Coherence.Log;
    public class EntityIDGenerator
    {
        public enum Error
        {
            None,
            OutOfIDs,
        }

        public ushort MaxIndex => endID;

        private ushort runningEntityID = 0;
        private readonly ushort startID;
        private readonly ushort endID;
        private readonly bool isAbsolute;

        private readonly Queue<Entity> reusableIDs = new Queue<Entity>();
        private readonly HashSet<ushort> reusableIDIndexes = new HashSet<ushort>();

        public EntityIDGenerator(ushort startID, ushort endID, bool isAbsolute, Logger logger)
        {
            if (startID == 0)
            {
                throw new Exception("can not use startID == 0");
            }

            runningEntityID = startID;

            if (endID > Entity.MaxID)
            {
                throw new Exception($"endID: {endID} is greater than MaxID: {Entity.MaxID}");
            }

            if (startID > endID)
            {
                throw new Exception($"invalid startID: {startID} and endID: {endID} -- endID should be greater than or equal to startID");
            }

            this.startID = startID;
            this.endID = endID;
            this.isAbsolute = isAbsolute;
        }

        public Error GetEntity(out Entity entity)
        {
            if (runningEntityID <= endID)
            {
                entity = new Entity(runningEntityID++, 0, isAbsolute);

                return Error.None;
            }

            if (reusableIDs.Count > 0)
            {
                entity = reusableIDs.Dequeue();
                reusableIDIndexes.Remove(entity.Index);

                return Error.None;
            }

            entity = isAbsolute ? Entity.InvalidAbsolute : Entity.InvalidRelative;

            return Error.OutOfIDs;
        }

        public void ReleaseEntity(Entity id)
        {
            if (id.Index < startID) throw new Exception($"trying to recycle invalid ID: {id} < {startID}");
            if (id.Index > endID) throw new Exception($"trying to recycle invalid ID: {id} > {endID}");
            if (id.Index >= runningEntityID) throw new Exception($"trying to recycle invalid ID: {id} not assigned");
            if (reusableIDIndexes.Contains(id.Index)) throw new Exception($"trying to recycle invalid ID: {id} already released");
            if (id.IsAbsolute != isAbsolute) throw new Exception($"trying to recycle incorrect type of entity. Expected {Entity.TypeToString(isAbsolute)} got {Entity.TypeToString(id.IsAbsolute)} ");

            reusableIDs.Enqueue(new Entity(id.Index, id.NextVersion, isAbsolute));
            reusableIDIndexes.Add(id.Index);
        }

        public void Reset()
        {
            runningEntityID = startID;
            reusableIDs.Clear();
            reusableIDIndexes.Clear();
        }
    }
}

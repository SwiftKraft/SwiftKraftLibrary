using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IPet : IEntity
    {
        public IEntity Owner { get; set; }

        public IEntity GetRootOwner()
        {
            HashSet<IEntity> visited = new();
            return GetRootOwner(visited);
        }

        private IEntity GetRootOwner(HashSet<IEntity> visited)
        {
            if (Owner is not IPet pet || visited.Contains(pet) || Owner == null || Owner == this)
                return Owner;
            visited.Add(pet);
            return pet.GetRootOwner();
        }
    }
}

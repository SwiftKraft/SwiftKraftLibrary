using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IPet : IPawn
    {
        public IPawn Owner { get; set; }

        public IPawn GetRootOwner()
        {
            HashSet<IPawn> visited = new();
            return GetRootOwner(visited);
        }

        private IPawn GetRootOwner(HashSet<IPawn> visited)
        {
            if (Owner is not IPet pet || visited.Contains(pet) || Owner == null || Owner == this)
                return Owner;
            visited.Add(pet);
            return pet.GetRootOwner();
        }
    }
}

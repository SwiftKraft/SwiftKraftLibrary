using System.Collections.Generic;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IPet : IPawn
    {
        public IPawn Owner { get; set; }
    }

    public static class IPetExtensions
    {
        public static IPawn GetRootOwner(this IPet pet)
        {
            HashSet<IPawn> visited = new();
            return GetRootOwnerHelper(pet, visited);
        }

        private static IPawn GetRootOwnerHelper(this IPet targetPet, HashSet<IPawn> visited)
        {
            if (targetPet.Owner is not IPet pet || visited.Contains(pet) || targetPet.Owner == null || targetPet.Owner == targetPet)
                return targetPet.Owner;
            visited.Add(pet);
            return pet.GetRootOwnerHelper(visited);
        }
    }
}

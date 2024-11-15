using UnityEngine;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IPawn : IFaction
    {
        public GameObject GameObject { get; }
    }
}

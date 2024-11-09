using UnityEngine;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface IEntity : IFaction
    {
        public GameObject GameObject { get; }
    }
}

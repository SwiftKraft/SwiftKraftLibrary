using UnityEngine;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface ILineOfSight : IPawn
    {
        public Transform[] SightPoints { get; }
    }
}

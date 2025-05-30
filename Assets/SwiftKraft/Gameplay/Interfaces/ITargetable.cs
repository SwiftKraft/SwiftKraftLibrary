using UnityEngine;

namespace SwiftKraft.Gameplay.Interfaces
{
    public interface ITargetable : IFaction, IDamagable, ILineOfSight
    {
        public int Priority { get; set; }

        public bool CanTarget { get; }
    }
}
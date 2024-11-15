using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Bases
{
    public abstract class PawnBehaviourBase : FactionBehaviourBase, IPawn
    {
        public GameObject GameObject => gameObject;
    }
}

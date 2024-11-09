using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Bases
{
    public abstract class EntityBehaviourBase : FactionBehaviourBase, IEntity
    {
        public GameObject GameObject => gameObject;
    }
}

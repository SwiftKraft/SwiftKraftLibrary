using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Bases
{
    public abstract class FactionBehaviourBase : MonoBehaviour, IFaction
    {
        public virtual string Faction { get; set; }
    }
}

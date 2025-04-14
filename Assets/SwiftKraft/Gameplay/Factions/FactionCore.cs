using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Factions
{
    public class FactionCore : MonoBehaviour, IFaction
    {
        [field: SerializeField]
        public string Faction { get; set; }

        public Faction FactionObject => Faction;
    }
}
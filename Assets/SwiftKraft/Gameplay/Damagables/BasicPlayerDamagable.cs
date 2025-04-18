using SwiftKraft.Gameplay.Factions;
using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Damagables
{
    public class BasicPlayerDamagable : BasicDamagable, ITargetable
    {
        public FactionCore FactionCore { get; set; }

        public int Priority { get => 1; set { } }

        [field: SerializeField]
        public Transform[] SightPoints { get; set; }

        public GameObject GameObject => gameObject;

        public string Faction { get => FactionCore.Faction; set => FactionCore.Faction = value; }

        protected override void Awake()
        {
            base.Awake();
            FactionCore = GetComponent<FactionCore>();
        }
    }
}

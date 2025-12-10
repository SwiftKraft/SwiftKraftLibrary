using SwiftKraft.Gameplay.Factions;
using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Damagables
{
    [RequireComponent(typeof(FactionCore))]
    public class BasicTargetableDamagable : BasicDamagable, ITargetable
    {
        public FactionCore FactionCore { get; set; }

        [field: SerializeField]
        public int Priority { get; set; } = 1;

        [field: SerializeField]
        public Transform[] SightPoints { get; set; }

        public GameObject GameObject => gameObject;

        public string Faction { get => FactionCore.Faction; set => FactionCore.Faction = value; }

        public bool CanTarget => !IsDead;

        public object GetStatus(string statusId) => null;

        protected override void Awake()
        {
            base.Awake();
            FactionCore = GetComponent<FactionCore>();
        }
    }
}

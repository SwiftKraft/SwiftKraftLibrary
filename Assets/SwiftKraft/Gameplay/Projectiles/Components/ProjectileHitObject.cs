using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class ProjectileHitObject : ProjectileComponent
    {
        public Spawner[] Spawners;

        public override void Init() => Component.OnHit += OnHit;

        protected virtual void OnDestroy() => Component.OnHit -= OnHit;

        protected virtual void OnHit(ProjectileBase.HitInfo info) => Spawners.GetWeightedRandom().Spawn(info.Position, Quaternion.LookRotation(info.Normal, Vector3.up));

        [Serializable]
        public class Spawner : IWeight
        {
            [field: SerializeField]
            public int Weight { get; private set; }
            public GameObject[] Spawnees;

            public void Spawn(Vector3 pos, Quaternion rot)
            {
                foreach (GameObject go in Spawnees)
                    Instantiate(go, pos, rot);
            }
        }
    }
}
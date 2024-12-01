using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Gameplay.Interfaces;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class SpreadPatternHolder : PetBehaviourBase, IVisualOrigin
    {
        public GameObject Prefab;
        public Spawner[] Projectiles;

        public Vector3 VisualOrigin { get; set; }

        protected override void OnOwnerChanged()
        {
            base.OnOwnerChanged();
            foreach (Spawner sp in Projectiles)
                sp.Spawn(this, Prefab);
            Destroy(gameObject);
        }

        [Serializable]
        public class Spawner
        {
            public GameObject OverridePrefab;
            public Vector3 Rotation;
            public float Spread;
            public int Count = 1;

            public void Spawn(SpreadPatternHolder pattern, GameObject originalPrefab)
            {
                for (int i = 0; i < Count; i++)
                {
                    Quaternion rot = pattern.transform.rotation * Quaternion.Euler(Rotation);

                    if (Spread > 0f)
                        rot *= Quaternion.Euler(Random.insideUnitCircle * Spread);

                    GameObject go = Instantiate(OverridePrefab != null ? OverridePrefab : originalPrefab, pattern.transform.position, rot);

                    if (go.TryGetComponent(out IPet pet))
                        pet.Owner = pattern.GetRootOwner();
                    if (go.TryGetComponent(out IVisualOrigin ori))
                        ori.VisualOrigin = pattern.VisualOrigin;
                }
            }
        }
    }
}

using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Gameplay.Interfaces;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public class SpreadPatternHolder : PetBehaviourBase
    {
        public GameObject Prefab;
        public Spawner[] Projectiles;

        protected override void OnOwnerChanged()
        {
            base.OnOwnerChanged();
            foreach (Spawner sp in Projectiles)
                sp.Spawn(this, Prefab);
        }

        [Serializable]
        public class Spawner
        {
            public GameObject OverridePrefab;
            public Vector3 Rotation;

            public void Spawn(SpreadPatternHolder pattern, GameObject originalPrefab)
            {
                GameObject go = Instantiate(OverridePrefab != null ? OverridePrefab : originalPrefab, pattern.transform.position, pattern.transform.rotation * Quaternion.Euler(Rotation));
                if (go.TryGetComponent(out IPet pet))
                    pet.Owner = pattern.GetRootOwner();
            }
        }
    }
}

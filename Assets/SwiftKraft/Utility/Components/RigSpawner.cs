using System;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Utils
{
    public class RigSpawner : MonoBehaviour
    {
        public GameObject Prefab;

        public RigDefinition ThisRig { get; private set; }

        public event Action<GameObject> OnSpawn;
        public UnityEvent<GameObject> OnSpawnEvent;

        private void Awake() => ThisRig = GetComponentInChildren<RigDefinition>();

        [ContextMenu("Spawn")]
        public void Spawn()
        {
            if (Prefab == null)
                return;

            GameObject spawnee = Instantiate(Prefab, transform.position, transform.rotation);

            if (spawnee.TryGetComponentInChildren(out RigDefinition rig))
                rig.Replicate(ThisRig);

            OnSpawn?.Invoke(spawnee);
            OnSpawnEvent?.Invoke(spawnee);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class SpawnerGroup : MonoBehaviour, ISpawner
    {
        public bool SpawnAll;
        public int Amount = 1;

        int lastRandom;

        public ISpawner[] Spawners { get; private set; }

        private void Awake() => Spawners = GetComponentsInChildren<ISpawner>();

        public void Spawn()
        {
            if (Spawners.Length <= 0)
                return;

            for (int i = 0; i < Amount; i++)
            {
                if (SpawnAll)
                {
                    foreach (ISpawner sp in Spawners)
                        sp.Spawn();
                    return;
                }

                Spawners.GetRandom(ref lastRandom).Spawn();
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class SpawnerGroup : MonoBehaviour, ISpawner
    {
        public bool SpawnAll;
        public int Amount = 1;

        int lastRandom;

        public List<ISpawner> Spawners { get; private set; }

        private void Awake()
        {
            Spawners = new(GetComponentsInChildren<ISpawner>());
            Spawners.Remove(this);
        }

        public void Spawn()
        {
            if (Spawners.Count <= 0)
                return;

            if (SpawnAll)
            {
                foreach (ISpawner sp in Spawners)
                    sp.Spawn();
                return;
            }

            List<ISpawner> candidates = new(Spawners);

            for (int i = 0; i < Amount; i++)
            {
                ISpawner sp = candidates.GetRandom(ref lastRandom);

                sp ??= Spawners.GetRandom(ref lastRandom);
                candidates.Remove(sp);
                sp.Spawn();
            }
        }
    }
}

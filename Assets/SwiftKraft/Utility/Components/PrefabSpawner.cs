using UnityEngine;

namespace SwiftKraft.Utils
{
    public class PrefabSpawner : MonoBehaviour, ISpawner
    {
        public GameObject Prefab;

        public virtual void Spawn() => Instantiate(Prefab, transform.position, transform.rotation);
    }
}

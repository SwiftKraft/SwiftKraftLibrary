using Newtonsoft.Json;
using SwiftKraft.Utils;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [field: SerializeField]
        public BuildingPrefabsRegistry Registry { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public GameObject GetPrefab(string id) => Registry.Entries.FirstOrDefault((ent) => ent.ID == id).Prefab;

        public bool TryGetPrefab(string id, out GameObject prefab)
        {
            prefab = GetPrefab(id);
            return prefab != null;
        }
    }
}

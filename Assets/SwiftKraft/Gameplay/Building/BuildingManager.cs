using Newtonsoft.Json;
using SwiftKraft.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        [field: SerializeField]
        public BuildingPrefabsRegistry Registry { get; private set; }

        public BuildScene CurrentScene
        {
            get
            {
                _currentScene ??= new();
                return _currentScene;
            }

            set
            {
                if (value == null)
                {
                    _currentScene = new();
                    return;
                }

                _currentScene = value;
            }
        }
        private BuildScene _currentScene;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void ReloadScene()
        {
            foreach (BuildInstance bi in CurrentScene.Builds)
                bi.Spawn();
        }

        public bool IsPrefabRegistered(GameObject prefab, out string id)
        {
            foreach (BuildingPrefabsRegistry.Entry ent in Registry.Entries)
                if (ent.Prefab == prefab)
                {
                    id = ent.ID;
                    return true;
                }
            id = "";
            return false;
        }

        public bool IsPrefabRegistered(GameObject prefab) => IsPrefabRegistered(prefab, out _);

        public GameObject Create(string id, TransformData transf)
        {
            BuildInstance bi = new(id, transf);
            GameObject sp = bi.Spawn();
            if (sp != null)
                CurrentScene.Builds.Add(bi);
            return sp;
        }

        public GameObject Create(GameObject prefab, TransformData transf) => !IsPrefabRegistered(prefab, out string id) ? null : Create(id, transf);

        public GameObject GetPrefab(string id) => Registry.Entries.FirstOrDefault((ent) => ent.ID == id).Prefab;

        public bool TryGetPrefab(string id, out GameObject prefab)
        {
            prefab = GetPrefab(id);
            return prefab != null;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class BuildScene
    {
        [JsonProperty]
        public List<BuildInstance> Builds = new();

        public void Remove(BuildInstance build) => Builds.Remove(build);
    }
}

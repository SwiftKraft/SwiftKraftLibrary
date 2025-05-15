using Newtonsoft.Json;
using SwiftKraft.Utils;
using System;
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
                    _currentScene.Clear();
                    _currentScene = new();
                    return;
                }

                if (_currentScene != value && _currentScene != null)
                    _currentScene.Clear();

                _currentScene = value;
                _currentScene.Refresh();
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

        public bool IsPrefabRegistered(GameObject prefab, out string id, out BuildingPrefabsRegistry.Entry entr)
        {
            foreach (BuildingPrefabsRegistry.Entry ent in Registry.Entries)
                if (ent.Prefab == prefab)
                {
                    id = ent.ID;
                    entr = ent;
                    return true;
                }
            id = "";
            entr = default;
            return false;
        }

        public bool IsPrefabRegistered(GameObject prefab, out string id) => IsPrefabRegistered(prefab, out id, out _);

        public bool IsPrefabRegistered(GameObject prefab) => IsPrefabRegistered(prefab, out _);

        public GameObject Create(string id, TransformData transf) => CurrentScene.Create(id, transf);

        public GameObject Create(GameObject prefab, TransformData transf) => !IsPrefabRegistered(prefab, out string id) ? null : Create(id, transf);

        public GameObject GetPrefab(string id) => Registry.Entries.FirstOrDefault((ent) => ent.ID == id).Prefab;

        public bool TryGetPrefab(string id, out GameObject prefab)
        {
            prefab = GetPrefab(id);
            return prefab != null;
        }

        public static Blueprint ToBlueprint(GameObject prefab)
        {
            if (Instance != null && Instance.IsPrefabRegistered(prefab, out _, out BuildingPrefabsRegistry.Entry ent) && ent.OverrideBlueprint != null)
            {
                GameObject b = Instantiate(ent.OverrideBlueprint);

                Blueprint m = b.TryGetComponent(out Blueprint p) ? p : b.AddComponent<Blueprint>();
                m.Renderers = b.GetComponentsInChildren<Renderer>();
                return m;
            }

            GameObject go = Instantiate(prefab);

            List<Renderer> l = new();
            foreach (Component c in go.GetComponentsInChildren<Component>())
                if (c is Renderer r)
                    l.Add(r);
                else if (c is Collider col)
                {
                    if (col is MeshCollider mCol)
                        mCol.convex = true;

                    col.isTrigger = true;
                }
                else if (c is not MeshFilter && c is not Transform)
                    c.CleanDestroy();

            Blueprint bp = go.AddComponent<Blueprint>();
            bp.Renderers = l.ToArray();

            return bp;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class BuildScene
    {
        [JsonProperty]
        public List<BuildInstance> Builds = new();

        public static event Action<BuildScene, BuildInstance> OnCreate;
        public static event Action<BuildScene, BuildInstance> OnRemove;

        public void Remove(BuildInstance build)
        {
            Builds.Remove(build);
            OnRemove?.Invoke(this, build);
        }

        public GameObject Create(string id, TransformData transf)
        {
            BuildInstance bi = new(id, transf);
            GameObject sp = bi.Spawn();
            if (sp != null)
                Builds.Add(bi);
            OnCreate?.Invoke(this, bi);
            return sp;
        }

        public void Clear()
        {
            for (int i = 0; i < Builds.Count; i++)
                Builds[i].Destroy();
            Builds.Clear();
        }

        public void Refresh()
        {
            for (int i = 0; i < Builds.Count; i++)
                Builds[i].Spawn();
        }
    }
}

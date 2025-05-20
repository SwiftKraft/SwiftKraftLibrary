using Newtonsoft.Json;
using SwiftKraft.Saving.Data;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class BuildInstance : SaveInstanceBase<BuildDataBase>
    {
        public GameObject Prefab => BuildingManager.Instance.TryGetPrefab(prefabID, out GameObject prefab) ? prefab : null;

        [JsonProperty("prefabID")]
        private readonly string prefabID;
        [JsonProperty("transform")]
        public SavableTransformData Transform;

        public BuildLinker Linker { get; private set; }

        [JsonConstructor]
        public BuildInstance(string prefabID, TransformData transform)
        {
            this.prefabID = prefabID;
            Transform = transform;
        }

        public GameObject Spawn()
        {
            if (Linker != null)
            {
                Linker.Refreshed = true;
                Object.DestroyImmediate(Linker.gameObject, false);
            }

            if (BuildingManager.Instance == null || Prefab == null)
                return null;

            GameObject go = Object.Instantiate(Prefab, Transform.Position, Transform.Rotation);

            Linker = go.TryGetComponent(out BuildLinker bl) ? bl : go.AddComponent<BuildLinker>();
            Linker.Init(this);
            return Linker.gameObject;
        }

        public void Destroy()
        {
            if (BuildingManager.Instance != null)
                BuildingManager.Instance.CurrentScene.Remove(this);
            Disposed = true;
            if (Linker != null)
                Object.Destroy(Linker.gameObject);
        }
    }

    public class BuildDataBase : SaveDataBase { }
}
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class BuildLinker : MonoBehaviour
    {
        public BuildInstance Instance { get; private set; }

        public bool Refreshed { get; set; }

        public void Init(BuildInstance instance) => Instance = instance;

        public void Refresh() => Instance.Transform = new TransformData(transform);

        public void Remove() => Instance.Destroy();
    }
}

using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class BuildLinker : MonoBehaviour
    {
        public BuildInstance Instance { get; private set; }

        public bool Refreshed { get; set; }

        public void Init(BuildInstance instance) => Instance = instance;

        private void OnDestroy()
        {
            if (!Refreshed)
                Instance.Destroy();
        }
    }
}

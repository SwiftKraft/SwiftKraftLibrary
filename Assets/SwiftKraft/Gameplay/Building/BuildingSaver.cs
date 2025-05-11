using Newtonsoft.Json;
using SwiftKraft.Saving.Progress;
using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    [RequireComponent(typeof(BuildingManager))]
    public class BuildingSaver : MonoBehaviour
    {
        public string ProgressableID = "Building";

        BuildingManager manager;

        private void Awake() => manager = GetComponent<BuildingManager>();

        [ContextMenu("Save")]
        public void Save()
        {
            if (ProgressManager.Current == null)
            {
                Debug.LogError("Failed to save build scene. Current progress save doesn't exist! ", this);
                return;
            }

            if(!ProgressManager.Current.TryGetProgressable(ProgressableID, out Progress p) && !ProgressManager.Current.TryAddProgressable(ProgressableID, out p))
            {
                Debug.LogError("Failed to save build scene. Progressable ID taken! ", this);
                return;
            }

            p.Scene = manager.CurrentScene;
            ProgressManager.SaveProgress();
        }

        [ContextMenu("Load")]
        public void Load()
        {
            if (ProgressManager.Current == null)
            {
                Debug.LogError("Failed to load build scene. Current progress save doesn't exist! ", this);
                return;
            }

            if (!ProgressManager.Current.TryGetProgressable(ProgressableID, out Progress p))
            {
                Debug.LogError("Failed to load build scene. Progressable ID doesn't exist! ", this);
                return;
            }

            manager.CurrentScene = p.Scene;
        }

        public class Progress : Progressable
        {
            [JsonProperty]
            public BuildScene Scene;
        }
    }
}

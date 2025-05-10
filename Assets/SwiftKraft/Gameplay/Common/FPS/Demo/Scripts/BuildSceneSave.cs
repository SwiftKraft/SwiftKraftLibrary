using SwiftKraft.Gameplay.Building;
using SwiftKraft.Saving.Progress;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [RequireComponent(typeof(BuildingSaver))]
    public class BuildSceneSave : MonoBehaviour
    {
        public string SaveID = "BuildingScene";
        BuildingSaver saver;

        private void Awake()
        {
            BuildScene.OnCreate += OnCreate;
            BuildScene.OnRemove += OnRemove;
        }

        private void OnRemove(BuildScene scene, BuildInstance obj) => saver.Save();

        private void OnCreate(BuildScene scene, BuildInstance obj) => saver.Save();

        private void Start()
        {
            saver = GetComponent<BuildingSaver>();
            if (!ProgressManager.LoadProgress(SaveID))
            {
                ProgressManager.CreateProgress(SaveID);
                saver.Save();
            }

            saver.Load();
        }

        private void OnDestroy()
        {
            ProgressManager.Current = null;

            BuildScene.OnCreate -= OnCreate;
            BuildScene.OnRemove -= OnRemove;
        }
    }
}

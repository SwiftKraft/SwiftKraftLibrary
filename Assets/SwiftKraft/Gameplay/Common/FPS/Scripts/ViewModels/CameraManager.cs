using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class CameraManager : MonoBehaviour
    {
        public Camera MainCamera;
        public Camera ViewModelCamera;
        public Transform ViewModelWorkspace;
        public CopyRotation CameraAnimations;

        public float ZoomTime = 0.1f;

        public FOVOverride ViewModelFOV;
        public FOVOverride MainCameraFOV;

        float velView;
        float velCam;

        public float TargetViewModelFOV => ViewModelFOV.GetFOV();
        public float CurrentViewModelFOV
        {
            get => ViewModelCamera != null ? ViewModelCamera.fieldOfView : 0f;
            private set
            {
                if (ViewModelCamera != null)
                    ViewModelCamera.fieldOfView = value;
            }
        }
        public float TargetCameraFOV => MainCameraFOV.GetFOV();
        public float CurrentCameraFOV
        {
            get => MainCamera != null ? MainCamera.fieldOfView : 0f;
            private set
            {
                if (MainCamera != null)
                    MainCamera.fieldOfView = value;
            }
        }

        private void Awake()
        {
            ViewModelFOV.Original = CurrentViewModelFOV;
            MainCameraFOV.Original = CurrentCameraFOV;
        }

        private void Update()
        {
            CurrentCameraFOV = Mathf.SmoothDamp(CurrentCameraFOV, TargetCameraFOV, ref velCam, ZoomTime);
            CurrentViewModelFOV = Mathf.SmoothDamp(CurrentViewModelFOV, TargetViewModelFOV, ref velView, ZoomTime);
        }

        public void SetViewModelFOV(float fov, bool forced = false)
        {
            if (forced)
                CurrentViewModelFOV = fov;
            ViewModelFOV.Original = fov;
        }

        public void SetCameraFOV(float fov, bool forced = false)
        {
            if (forced)
                CurrentCameraFOV = fov;
            MainCameraFOV.Original = fov;
        }

        [Serializable]
        public class FOVOverride
        {
            public float Original = 90f;

            public readonly List<Override> Overrides = new();

            public float GetFOV()
            {
                Override target = null;
                foreach (Override o in Overrides)
                    if (o.Active && (target == null || target.Priority <= o.Priority))
                        target = o;
                return target != null ? target.FOV : Original;
            }

            public Override AddOverride(float fov, int priority = 0)
            {
                Override o = new() { FOV = fov, Priority = priority };
                Overrides.Add(o);
                return o;
            }

            public void RemoveOverride(Override ov) => Overrides.Remove(ov);

            public void ClearOverrides() => Overrides.Clear();

            public class Override
            {
                public float FOV;
                public int Priority;
                public bool Active;
            }
        }
    }
}

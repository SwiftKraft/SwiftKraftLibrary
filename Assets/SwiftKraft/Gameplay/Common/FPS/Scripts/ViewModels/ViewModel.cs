using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class ViewModel : MonoBehaviour
    {
        public CameraManager Parent { get; private set; }

        public float FOV = 60f;
        public float CamMultiplier = 1f;

        public Transform CameraTarget;

        private void Awake()
        {
            Parent = GetComponentInParent<CameraManager>();
            Initialize();
        }

        private void OnEnable() => Initialize();

        public void Initialize()
        {
            Parent.SetViewModelFOV(FOV, true);
            Parent.CameraAnimations.TargetTransform = CameraTarget;
            Parent.CameraAnimations.Multiplier = CamMultiplier;
        }
    }
}

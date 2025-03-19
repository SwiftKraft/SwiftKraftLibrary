using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class ViewModel : MonoBehaviour
    {
        public CameraManager Parent { get; private set; }

        public float FOV = 60f;
        public Vector4 CamAxisMultiplier = Vector4.one;
        public float CamMultiplier = 1f;
        public bool Local;
        public Vector3 Offset;

        public Transform CameraTarget;
        public Transform ReferenceTarget;

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
            Parent.CameraAnimations.ReferenceTransform = ReferenceTarget;
            Parent.CameraAnimations.AxisMultiplier = CamAxisMultiplier;
            Parent.CameraAnimations.Multiplier = CamMultiplier;
            Parent.CameraAnimations.Offset = Offset;
            Parent.CameraAnimations.Local = Local;
        }
    }
}

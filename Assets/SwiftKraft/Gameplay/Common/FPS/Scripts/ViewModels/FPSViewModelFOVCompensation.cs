using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class FPSViewModelFOVCompensation : OptionalModifyTransformComponent
    {
        public CameraManager CameraManager { get; private set; }
        public Transform ReferenceTransform;
        public Vector4 AxisMultiplier = Vector4.one;

        protected bool IsChild;
        protected float Ratio => CameraManager != null ? CameraManager.MainCameraFOV.GetFOV() / CameraManager.ViewModelFOV.GetFOV() : 1f;

        protected override void Awake()
        {
            ModifyTarget = transform;
            base.Awake();
            CameraManager = GetComponentInParent<CameraManager>();

            if (ReferenceTransform == null)
            {
                enabled = false;
                return;
            }

            IsChild = ReferenceTransform.GetComponentInChildren<FPSViewModelFOVCompensation>() == this;
        }

        protected virtual void Update()
        {
            Quaternion target = Quaternion.Inverse(Quaternion.LerpUnclamped(Quaternion.identity, ReferenceTransform.localRotation, Ratio)) * ReferenceTransform.localRotation;
            target.x *= AxisMultiplier.x;
            target.y *= AxisMultiplier.y;
            target.z *= AxisMultiplier.z;
            target.w *= AxisMultiplier.w;
            Rotation = target;
        }
    }
}

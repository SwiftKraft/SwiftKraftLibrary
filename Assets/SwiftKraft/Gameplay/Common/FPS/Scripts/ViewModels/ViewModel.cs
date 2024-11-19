using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class ViewModel : MonoBehaviour
    {
        public ViewModelManager Parent { get; private set; }

        public float FOV = 60f;

        public Transform ViewModelCameraTarget;

        public readonly BooleanLock CanUnequip = new();

        private void Awake()
        {
            Parent = GetComponentInParent<ViewModelManager>();
            Initialize();
        }

        private void OnEnable() => Initialize();

        public void Initialize()
        {
            Parent.SetFOV(FOV);
            Parent.CameraAnimations.TargetTransform = ViewModelCameraTarget;
        }
    }
}

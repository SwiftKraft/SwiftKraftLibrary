using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class ViewModel : MonoBehaviour
    {
        public ViewModelManager Parent { get; private set; }

        public float FOV = 60f;

        public readonly BooleanLock CanUnequip = new();

        private void Awake()
        {
            Parent = GetComponentInParent<ViewModelManager>();
            Parent.SetFOV(FOV);
        }

        private void OnEnable() => Parent.SetFOV(FOV);
    }
}

using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class ViewModel : MonoBehaviour
    {
        public ViewModelManager Parent { get; private set; }

        public float FOV = 60f;

        private void Awake()
        {
            Parent = GetComponentInParent<ViewModelManager>();
            Parent.SetFOV(FOV);
        }

        private void OnEnable() => Parent.SetFOV(FOV);
    }
}

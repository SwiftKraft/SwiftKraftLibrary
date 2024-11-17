using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class ViewModelManager : MonoBehaviour
    {
        public Camera Camera;
        public Transform Workspace;

        public void SetFOV(float fov)
        {
            if (Camera != null)
                Camera.fieldOfView = fov;
        }
    }
}

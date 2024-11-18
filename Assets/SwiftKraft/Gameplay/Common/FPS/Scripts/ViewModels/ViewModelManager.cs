using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class ViewModelManager : MonoBehaviour
    {
        public Camera MainCamera;
        public Camera ViewModelCamera;
        public Transform Workspace;

        public void SetFOV(float fov)
        {
            if (ViewModelCamera != null)
                ViewModelCamera.fieldOfView = fov;
        }
    }
}

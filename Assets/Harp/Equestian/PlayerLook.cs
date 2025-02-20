using SwiftKraft.Saving.Settings;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player.Movement
{
    public class PlayerLook : MonoBehaviour
    {
        #region Variables

        public const string SensKey = "c_sensitivity";

        public static readonly List<OverrideLayer> Overrides = new();

        public float worldUITurnTime = 0.2f;

        //public static float Sensitivity => SettingsManager.Current.TrySetting(SensKey, out SingleSetting<float> setting) ? setting.Value : 1f;

        public Transform cameraRoot;
        public Transform vCam;

        public Camera playerCamera;
        public Camera ViewModelCamera;

        public float Tilt
        {
            get => tilt;
            private set
            {
                tilt = value;
                cameraRoot.localRotation = Quaternion.Euler(cameraRoot.localEulerAngles.x, cameraRoot.localEulerAngles.y, tilt);
            }
        }

        [HideInInspector]
        public float TargetTilt;
        public float TiltSmoothTime = 0.05f;

        [SerializeField]
        private float Sensitivity = 12f;

        float currSens;

        float mouseX, mouseY;

        [SerializeField]
        float xClamp = 85f;

        float xRotation = 0f;
        float yRotation = 0f;

        float tiltVel;
        float tilt;

        float xVel;
        float yVel;

        Vector3 offsetStart;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            currSens = Sensitivity;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            offsetStart = transform.eulerAngles;
        }

        private void OnEnable()
        {
            offsetStart = transform.eulerAngles;
        }

        private void Update()
        {
           

            
            
                currSens = Sensitivity;

                foreach (OverrideLayer layer in Overrides)
                {
                    if (layer.Sensitivity > 0)
                    {
                        currSens = layer.Sensitivity;
                        break;
                    }
                }
            

            Vector2 mouseInput = new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            mouseX = mouseInput.x * currSens;
            mouseY = mouseInput.y * currSens;

            Vector3 targetRotation = transform.eulerAngles, YRotation = transform.eulerAngles;

            yRotation += mouseX;
            YRotation.y = yRotation + offsetStart.y;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -xClamp, xClamp);
            targetRotation.x = xRotation + offsetStart.x;

            UpdateMouseLook(targetRotation, YRotation);

            Tilt = Mathf.SmoothDamp(Tilt, TargetTilt, ref tiltVel, TiltSmoothTime);
        }

        #endregion

        public void UpdateMouseLook(Vector3 targetRotation, Vector3 YRotation)
        {
            transform.eulerAngles = YRotation;
            cameraRoot.eulerAngles = targetRotation;
        }

        public static void RemoveOverride(OverrideLayer layer) => Overrides.Remove(layer);

        public static OverrideLayer SetOverride(float value, int weight)
        {
            OverrideLayer layer = new(value, weight);
            Overrides.Add(layer);
            Overrides.OrderBy((l) => l.Weight);
            return layer;
        }

        public class OverrideLayer
        {
            public float Sensitivity;
            public int Weight;

            public OverrideLayer(float sens, int weight)
            {
                Sensitivity = sens;
                Weight = weight;
            }
        }
    }
}
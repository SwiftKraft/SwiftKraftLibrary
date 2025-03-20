using SwiftKraft.Gameplay.Weapons;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class OffsetSetterObject : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool DebugMode;
#endif

        public Vector3 TargetOffset;
        public Vector3 TargetEulerOffset;

        public WeaponAdsOffset Component
        {
            get
            {
                if (_component == null)
                    _component = GetComponentInParent<WeaponAdsOffset>();

                return _component;
            }
        }
        WeaponAdsOffset _component;

        private void Start()
        {
            Component.TargetPosition = TargetOffset;
            Component.TargetRotation = Quaternion.Euler(TargetEulerOffset);
        }

        private void OnDestroy()
        {
            Component.TargetPosition = Component.OriginalPosition;
            Component.TargetRotation = Component.OriginalRotation;
        }

        private void OnEnable()
        {
            Component.TargetPosition = TargetOffset;
            Component.TargetRotation = Quaternion.Euler(TargetEulerOffset);
        }

        private void OnDisable()
        {
            Component.TargetPosition = Component.OriginalPosition;
            Component.TargetRotation = Component.OriginalRotation;
        }
#if UNITY_EDITOR
        private void Update()
        {
            if (DebugMode)
            {
                Component.TargetPosition = TargetOffset;
                Component.TargetRotation = Quaternion.Euler(TargetEulerOffset);
            }
        }
#endif
    }
}

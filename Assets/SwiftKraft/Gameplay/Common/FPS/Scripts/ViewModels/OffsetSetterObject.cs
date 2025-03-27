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

        WeaponAdsOffset.Override offset;

        private void Awake() => offset = GetComponentInParent<WeaponAdsOffset>().AddOverride();

        private void Start()
        {
            offset.TargetPosition = TargetOffset;
            offset.TargetRotation = Quaternion.Euler(TargetEulerOffset);
        }

        private void OnDestroy() => offset.Dispose();

        private void OnEnable()
        {
            offset.TargetPosition = TargetOffset;
            offset.TargetRotation = Quaternion.Euler(TargetEulerOffset);
        }

        private void OnDisable()
        {
            offset.TargetPosition = default;
            offset.TargetRotation = new(0f, 0f, 0f, 1f);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (DebugMode)
            {
                offset.TargetPosition = TargetOffset;
                offset.TargetRotation = Quaternion.Euler(TargetEulerOffset);
            }
        }
#endif
    }
}

using SwiftKraft.Gameplay.Weapons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class OffsetSetterObject : MonoBehaviour
    {
        public Vector3 TargetOffset;

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
            Component.TargetLocation = TargetOffset;
        }

        private void OnDestroy()
        {
            Component.TargetLocation = Component.OriginalLocation;
        }

        private void OnEnable()
        {
            Component.TargetLocation = TargetOffset;
        }

        private void OnDisable()
        {
            Component.TargetLocation = Component.OriginalLocation;
        }
    }
}

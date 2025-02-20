using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponAdsOffset : OptionalModifyTransformComponent
    {
        public WeaponAds Component
        {
            get
            {
                if (_component == null)
                    _component = GetComponent<WeaponAds>();

                return _component;
            }
        }
        WeaponAds _component;

        public Vector3 TargetLocation { get; set; }
        public Vector3 OriginalLocation { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            OriginalLocation = ModifyTarget.localPosition;
        }

        private void Update()
        {
            Position = Vector3.Lerp(OriginalLocation, TargetLocation, Component.Aiming);
        }
    }
}

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

        public Vector3 TargetPosition { get; set; }
        public Vector3 OriginalPosition { get; private set; }
        public Quaternion TargetRotation { get; set; } = new(0f, 0f, 0f, 1f);
        public Quaternion OriginalRotation { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            TargetPosition = Position;
            OriginalPosition = Position;
            TargetRotation = Rotation;
            OriginalRotation = Rotation;
        }

        private void Update()
        {
            Position = Vector3.Lerp(OriginalPosition, TargetPosition, Component.Aiming);
            Rotation = Quaternion.Lerp(OriginalRotation, TargetRotation, Component.Aiming);
        }
    }
}

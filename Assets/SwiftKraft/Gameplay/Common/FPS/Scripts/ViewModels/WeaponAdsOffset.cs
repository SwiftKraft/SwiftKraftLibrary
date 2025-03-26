using SwiftKraft.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAdsOffset : OptionalModifyTransformComponent
    {
        public class Override
        {
            public readonly WeaponAdsOffset Parent;
            public Vector3 TargetPosition { get; set; }
            public Quaternion TargetRotation { get; set; } = new(0f, 0f, 0f, 1f);

            public void Dispose() => Parent.RemoveOverride(this);

            public Override(WeaponAdsOffset parent) => Parent = parent;
        }

        public struct OverrideData
        {
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;

            public OverrideData(Vector3 tp, Quaternion tr)
            {
                TargetPosition = tp;
                TargetRotation = tr;
            }
        }

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

        public readonly List<Override> Overrides = new();

        public OverrideData Target 
        { 
            get
            {
                Vector3 resultPos = default;
                Quaternion resultRot = new(0f, 0f, 0f, 1f);
                foreach (Override ov in Overrides)
                {
                    resultPos += ov.TargetPosition;
                    resultRot *= ov.TargetRotation;
                }
                return new(resultPos, resultRot);
            }
        }
        public Vector3 OriginalPosition { get; private set; }
        public Quaternion OriginalRotation { get; private set; }

        protected Override BaseOverride;

        protected override void Awake()
        {
            base.Awake();
            OriginalPosition = Position;
            OriginalRotation = Rotation;
            BaseOverride = AddOverride();
            BaseOverride.TargetPosition = OriginalPosition;
            BaseOverride.TargetRotation = OriginalRotation;
        }

        protected virtual void Update()
        {
            Position = Vector3.Lerp(OriginalPosition, Target.TargetPosition, Component.Aiming);
            Rotation = Quaternion.Lerp(OriginalRotation, Target.TargetRotation, Component.Aiming);
        }

        public Override AddOverride()
        {
            Override ov = new(this);
            Overrides.Add(ov);
            return ov;
        }

        public void RemoveOverride(Override ov) => Overrides.Remove(ov);
    }
}

using SwiftKraft.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponShootPoint : WeaponComponent, IOverrideParent
    {
        public class Override : OverrideBase<WeaponShootPoint>
        {
            private Vector3 overridePosition;

            public Override(WeaponShootPoint parent) : base(parent) { }

            public Vector3 OverridePosition
            {
                get => overridePosition;
                set
                {
                    overridePosition = value;
                    Parent.UpdatePosition();
                }
            }
        }

        public readonly List<Override> Overrides = new();

        public Transform ShootPoint;

        Vector3 original;

        private void Awake() => original = ShootPoint.localPosition;

        public void UpdatePosition()
        {
            ShootPoint.localPosition = original;

            foreach (Override ov in Overrides)
                ShootPoint.localPosition += ov.OverridePosition;
        }

        public Override AddOverride()
        {
            Override ov = new(this);
            Overrides.Add(ov);
            return ov;
        }

        public void RemoveOverride(object ov) => Overrides.Remove((Override)ov);
    }
}

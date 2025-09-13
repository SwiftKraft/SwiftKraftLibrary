using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Animation
{
    public class AnimatorAmmoSetter : AnimatorFloatSetterBase
    {
        public IAmmo Ammo { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Ammo = GetComponentInParent<IAmmo>();
        }

        public override Interpolater AssignInterpolater() => null;

        public override float GetMaxValue() => Ammo.CurrentAmmo;
    }
}

using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Animation
{
    public class AnimatorAimSetter : AnimatorFloatSetterBase
    {
        public IAimable Aimable { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Aimable = GetComponentInParent<IAimable>();
        }

        public override Interpolater AssignInterpolater() => null;

        public override float GetMaxValue() => Aimable.AimProgress;
    }
}

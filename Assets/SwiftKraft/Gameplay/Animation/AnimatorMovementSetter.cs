using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Animation
{
    public class AnimatorMovementSetter : AnimatorFloatSetterBase
    {
        public new SmoothDampInterpolater Interpolater;

        public MotorBase Motor { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Motor = GetComponentInParent<MotorBase>();
        }

        public override Interpolater AssignInterpolater() => Interpolater;

        public override float GetMaxValue() => Motor.State;
    }
}

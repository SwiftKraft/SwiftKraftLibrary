using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Animation
{
    public class AnimatorMovementSetter : AnimatorFloatSetterBase
    {
        public new SmoothDampInterpolater Interpolater;

        [Header("Speed Changes")]
        public string SpeedParameterName = "MovementMultiplier";
        public bool Raw;
        public float Multiplier = 1f;
        public StateMultiplier[] StateMultipliers;

        private readonly Dictionary<int, float> multipliers = new();

        public MotorBase Motor { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Motor = GetComponentInParent<MotorBase>();
            foreach (StateMultiplier state in StateMultipliers)
                multipliers.Add(state.State, state.Multiplier);
        }

        protected override void Update()
        {
            base.Update();

            Animator.SetFloatSafe(SpeedParameterName, (Raw ? Motor.RawMoveFactorRate : Motor.MoveFactorRate) * (!multipliers.ContainsKey(Motor.State) ? 1f : multipliers[Motor.State]) * Multiplier);
        }

        public override Interpolater AssignInterpolater() => Interpolater;

        public override float GetMaxValue() => Motor.State;

        [Serializable]
        public class StateMultiplier
        {
            public int State;
            public float Multiplier;
        }
    }
}

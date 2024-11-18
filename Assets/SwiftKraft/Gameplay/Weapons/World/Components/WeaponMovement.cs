using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponMovement : WeaponComponentBlocker
    {
        public MotorBase Motor { get; protected set; }

        public virtual float State => Motor.State;

        public int[] AttackBlockStates;

        protected override void Awake()
        {
            base.Awake();
            Motor = GetComponentInParent<MotorBase>();
            if (Motor == null)
                enabled = false;
        }

        protected virtual void FixedUpdate() => AttackDisabler.Active = AttackBlockStates.Contains(Motor.State);
    }

    public abstract class WeaponMovement<T> : WeaponMovement where T : Interpolater, new()
    {
        public T MovementInterpolater;

        public override float State => MovementInterpolater.CurrentValue;

        protected virtual void Update()
        {
            MovementInterpolater.MaxValue = Motor.State;
            MovementInterpolater.Tick(Time.deltaTime);
        }
    }
}

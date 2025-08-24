using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public class MoonsHauntedMotor : CharacterControllerMotor
    {
        public static readonly List<MoonsHauntedMotor> All = new();

        public float SprintSpeed = 7f;

        public Vector3 RequestedVelocity { get; set; }
        [field: SerializeField]
        public float RequestedVelocityDecay { get; set; } = 10f;

        public override float CurrentSpeed => WishSprint ? SprintSpeed : MoveSpeed;
        public override float MoveFactorRate => CurrentSpeed;
        public override float RawMoveFactorRate => CurrentSpeed / MoveSpeed;

        public bool WishSprint { get; set; }

        public bool CanBePushed { get; set; }

        protected override void Awake()
        {
            All.Add(this);
            base.Awake();
        }

        protected override void FixedUpdate()
        {
            RequestedVelocity = Vector3.MoveTowards(RequestedVelocity, Vector3.zero, RequestedVelocityDecay * Time.fixedDeltaTime);

            base.FixedUpdate();

            State += WishSprint ? 1 : 0;
            Debug.DrawRay(LookPoint.position, LookPoint.forward, Color.blue, Time.fixedDeltaTime);
        }

        protected virtual void OnDestroy() => All.Remove(this);

        public override void Move(Vector3 direction)
        {
            base.Move(direction);

            if (CanBePushed)
                Component.Move(RequestedVelocity * Time.fixedDeltaTime);

            if (direction.sqrMagnitude > 0f)
                PushOthers();
        }

        public void PushOthers()
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i] != null)
                {
                    Vector3 dirRaw = All[i].transform.position - transform.position;
                    float dist = dirRaw.magnitude;
                    if (dist < 1f)
                    {
                        Vector3 dir = dirRaw.normalized;
                        float magnitude = (1f - dist) * MoveSpeed;
                        All[i].RequestedVelocity = dir * magnitude;
                    }
                }
        }
    }
}

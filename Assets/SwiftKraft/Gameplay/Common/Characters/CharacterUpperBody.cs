using SwiftKraft.Gameplay.Motors;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.Characters
{
    public class CharacterUpperBody : MonoBehaviour
    {
        [Serializable]
        public class Bone
        {
            public Transform Reference;
            public float Influence;
            public Vector3 AngleOffset;
        }

        public MotorBase Motor { get; private set; }

        public Bone[] Bones;

        public float AdjustAngleMin = -50f;
        public float AdjustAngleMax = 50f;
        public float AdjustTime = 0.1f;
        public float AdjustOffset = 15f;

        public Vector2 LookRotation => Motor.CurrentLookRotation.eulerAngles;
        public float BottomRotationOffset { get; private set; }
        public float CurrentBottomRotation { get; private set; }
        public float TargetBottomRotation { get; private set; }

        private void Awake()
        {
            Motor = GetComponentInParent<MotorBase>();
            BottomRotationOffset = transform.localRotation.eulerAngles.y;
            TargetBottomRotation = transform.rotation.eulerAngles.y;
            CurrentBottomRotation = TargetBottomRotation;
        }

        float botRotVel;

        private void LateUpdate()
        {
            if (Motor == null)
                return;

            if (Mathf.Abs(CurrentBottomRotation - TargetBottomRotation) > 0.001f)
                CurrentBottomRotation = Mathf.SmoothDampAngle(CurrentBottomRotation, TargetBottomRotation, ref botRotVel, AdjustTime);
            else if (CurrentBottomRotation != TargetBottomRotation)
                CurrentBottomRotation = TargetBottomRotation;

            transform.rotation = Quaternion.Euler(0f, CurrentBottomRotation, 0f);

            float curAngDelta = BottomRotationOffset + LookRotation.y - TargetBottomRotation;
            if (curAngDelta >= AdjustAngleMax || curAngDelta <= AdjustAngleMin || Motor.Moving)
                TargetBottomRotation = BottomRotationOffset + LookRotation.y + AdjustOffset;

            foreach (Bone bone in Bones)
                bone.Reference.rotation = Quaternion.SlerpUnclamped(bone.Reference.rotation, Motor.CurrentLookRotation, bone.Influence) * Quaternion.Euler(bone.AngleOffset);
        }
    }
}

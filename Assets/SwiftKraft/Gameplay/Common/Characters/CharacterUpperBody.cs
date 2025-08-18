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
            public float XInfluence;
            public float YInfluence;
        }

        public MotorBase Motor { get; private set; }

        public Bone[] Bones;

        public float AdjustAngle = 35f;
        public float AdjustTime = 0.1f;

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

            if (Mathf.Abs(BottomRotationOffset + LookRotation.y - TargetBottomRotation) >= AdjustAngle || Motor.Moving)
                TargetBottomRotation = BottomRotationOffset + LookRotation.y;

            foreach (Bone bone in Bones)
            {
                // Get delta between current and target
                Quaternion delta = Motor.CurrentLookRotation * Quaternion.Inverse(bone.Reference.rotation);

                // Extract euler from delta
                Vector3 deltaEuler = delta.eulerAngles;

                // Apply influence only to X and Y
                float newX = Mathf.LerpAngle(0, deltaEuler.x, bone.XInfluence);
                float newY = Mathf.LerpAngle(0, deltaEuler.y, bone.YInfluence);

                // Build partial delta back
                Quaternion partialDelta = Quaternion.Euler(newX, newY, 0);

                // Apply to reference
                bone.Reference.rotation = partialDelta * bone.Reference.rotation;
            }
        }
    }
}

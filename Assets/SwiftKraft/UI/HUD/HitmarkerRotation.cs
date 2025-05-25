using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.UI.HUD
{
    public class HitmarkerRotation : HitmarkerModule
    {
        public float Multiplier;
        public float RandomnessMax;
        public float RandomnessMin;
        public AnimationCurve Rotation;

        public RectTransform Rect;

        float baseRot;
        float random;

        public override void Init()
        {
            base.Init();
            baseRot = Rect.localEulerAngles.z;
        }

        public override void Trigger()
        {
            base.Trigger();
            if (RandomnessMin != RandomnessMax)
                random = Random.Range(RandomnessMin, RandomnessMax);
        }


        public override void Frame() => SetRotation(Sample(Hitmarker.Instance.Normalized));

        public void SetRotation(float rot) => Rect.localEulerAngles = new Vector3(0f, 0f, baseRot + rot);

        public float Sample(float normalized)
        {
            float m = (Rotation.Evaluate(normalized) * Multiplier) + 1f;

            if (RandomnessMax != RandomnessMin)
                m += random;

            return m;
        }
    }
}

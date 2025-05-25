using UnityEngine;
using Random = UnityEngine.Random;

namespace SwiftKraft.UI.HUD
{
    public class HitmarkerSize : HitmarkerModule
    {
        public float Multiplier;
        public float RandomnessMax;
        public float RandomnessMin;
        public AnimationCurve Size;

        public RectTransform Rect;

        Vector2 basePixels;
        float random;

        public override void Init()
        {
            base.Init();
            basePixels = Rect.sizeDelta;
        }

        public override void Trigger()
        {
            base.Trigger();
            if (RandomnessMin != RandomnessMax)
                random = Random.Range(RandomnessMin, RandomnessMax);
        }


        public override void Frame() => SetSize(Sample(Hitmarker.Instance.Normalized));

        public void SetSize(float size) => Rect.sizeDelta = size * basePixels;

        public float Sample(float normalized)
        {
            float m = (Size.Evaluate(normalized) * Multiplier) + 1f;

            if (RandomnessMax != RandomnessMin)
                m += random;

            return m;
        }
    }
}

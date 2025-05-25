using UnityEngine;
using Random = UnityEngine.Random;

namespace SwiftKraft.UI.HUD
{
    public class HitmarkerPosition : HitmarkerModule
    {
        public float Multiplier;
        public float RandomnessMax;
        public float RandomnessMin;

        public bool AllowNegative;
        public AnimationCurve PositionX;
        public AnimationCurve PositionY;

        public RectTransform Rect;

        Vector2 basePosition;
        Vector2 random;
        bool negativeX;
        bool negativeY;

        public override void Init()
        {
            base.Init();

            basePosition = Rect.anchoredPosition;
        }

        public override void Trigger()
        {
            base.Trigger();

            if (RandomnessMax != RandomnessMin)
            {
                Vector2 rand = Random.insideUnitCircle * (RandomnessMax - RandomnessMin);
                rand += rand.normalized * RandomnessMin;
                random = rand;
            }

            if (AllowNegative)
            {
                negativeX = Random.Range(0, 2) == 0;
                negativeY = Random.Range(0, 2) == 0;
            }
        }

        public override void Frame() => SetPosition(Sample(Hitmarker.Instance.Normalized));

        public void SetPosition(Vector2 pos) => Rect.anchoredPosition = pos + basePosition;

        public Vector2 Sample(float normalized)
        {
            Vector2 pos = new Vector2(PositionX.Evaluate(normalized) * (negativeX ? -1f : 1f), PositionY.Evaluate(normalized) * (negativeY ? -1f : 1f)) * Multiplier;

            if (RandomnessMax != RandomnessMin)
                pos += random;

            return pos;
        }
    }
}

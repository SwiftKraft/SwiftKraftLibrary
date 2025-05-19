using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.UI.HUD
{
    public class HitmarkerColor : HitmarkerModule
    {
        public Gradient Gradient;

        public Graphic[] Graphics;

        public override void Frame() => SetColor(Gradient.Evaluate(Hitmarker.Instance.Normalized));

        public void SetColor(Color color)
        {
            foreach (Graphic gra in Graphics)
                gra.color = color;
        }
    }
}

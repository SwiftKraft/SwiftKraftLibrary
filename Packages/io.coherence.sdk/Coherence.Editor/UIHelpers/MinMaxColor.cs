// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using Coherence.Toolkit;

    internal class MinMaxColor
    {
        public Color MinColor { get; }
        public Color MaxColor { get; }

        public MinMaxColor(Color minColor, Color maxColor)
        {
            MinColor = minColor;
            MaxColor = maxColor;
        }

        public Color Lerp(float amount, bool useHSV)
        {
            return useHSV ? Color.Lerp(MinColor, MaxColor, amount) : UIHelpers.HSVLerp(MinColor, MaxColor, amount);
        }
    }
}

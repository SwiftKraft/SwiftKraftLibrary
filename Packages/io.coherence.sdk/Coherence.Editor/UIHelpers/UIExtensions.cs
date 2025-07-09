// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;

    internal static class UIExtensions
    {
        public static Rect SplitX(this Rect rect, bool leftSide, float leftSideSize)
        {
            leftSideSize = Mathf.Clamp(leftSideSize, 0, rect.width);
            return leftSide
                ? new Rect(rect.x, rect.y, leftSideSize, rect.height)
                : new Rect(rect.x + leftSideSize, rect.y, rect.width - leftSideSize, rect.height);
        }

        public static Rect SplitXFraction(this Rect rect, bool leftSide, float fraction)
        {
            float leftSideSize = Mathf.Clamp01(fraction) * rect.width;
            return rect.SplitX(leftSide, leftSideSize);
        }
    }
}

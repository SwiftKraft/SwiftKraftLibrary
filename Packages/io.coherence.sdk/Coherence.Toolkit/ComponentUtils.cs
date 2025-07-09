// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;

    internal static class ComponentUtils
    {
        public static T SelfOrNull<T>(this T self) where T : Object
        {
            if (self != null)
            {
                return self;
            }

            return null;
        }
    }
}

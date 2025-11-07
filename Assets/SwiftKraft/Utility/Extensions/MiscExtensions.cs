using System;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public static class MiscExtensions
    {
        public static bool TryGetComponentInChildren<T>(this Component comp, out T component)
        {
            component = comp.GetComponentInChildren<T>();
            return component != null;
        }

        public static bool TryGetComponentInParent<T>(this Component comp, out T component)
        {
            component = comp.GetComponentInParent<T>();
            return component != null;
        }

        public static bool TryGetComponentInChildren<T>(this GameObject go, out T component)
        {
            component = go.GetComponentInChildren<T>();
            return component != null;
        }

        public static bool TryGetComponentInParent<T>(this GameObject go, out T component)
        {
            component = go.GetComponentInParent<T>();
            return component != null;
        }

        public static bool InLayerMask(this GameObject obj, LayerMask mask) => obj.layer.InLayerMask(mask);
        public static bool InLayerMask(this int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        public static Transform FindRecursive(this Transform parent, string childName)
        {
            foreach (Transform child in parent)
                if (child.name == childName)
                    return child;
                else
                {
                    Transform found = FindRecursive(child, childName);
                    if (found != null)
                        return found;
                }

            return null;
        }

        public static float GridSnap(this float value, float grid, float offset = default) => grid != 0f ? Mathf.Round(value / grid) * grid + offset : value;
        public static double GridSnap(this double value, double grid, double offset = default) => grid != 0d ? Math.Round(value / grid) * grid + offset : value;
        public static decimal GridSnap(this decimal value, decimal grid, decimal offset = default) => grid != 0m ? Math.Round(value / grid) * grid + offset : value;

        public static int GetSign(this int value) => value switch
        {
            > 0 => 1,
            < 0 => -1,
            _ => 0,
        };

        public static short GetSign(this short value) => value switch
        {
            > 0 => 1,
            < 0 => -1,
            _ => 0,
        };

        public static sbyte GetSign(this sbyte value) => value switch
        {
            > 0 => 1,
            < 0 => -1,
            _ => 0,
        };

        public static long GetSign(this long value) => value switch
        {
            > 0L => 1L,
            < 0L => -1L,
            _ => 0L,
        };

        public static float GetSign(this float value) => value switch
        {
            > 0f => 1f,
            < 0f => -1f,
            _ => 0f,
        };

        public static double GetSign(this double value) => value switch
        {
            > 0d => 1d,
            < 0d => -1d,
            _ => 0d,
        };

        public static decimal GetSign(this decimal value) => value switch
        {
            > 0M => 1M,
            < 0M => -1M,
            _ => 0M,
        };
    }
}

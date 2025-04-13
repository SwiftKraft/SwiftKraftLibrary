using System.Collections.Generic;
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

        public static bool InLayerMask(this GameObject obj, LayerMask mask) => obj.layer.InLayerMask(mask);
        public static bool InLayerMask(this int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

        public static bool HasParameter(this Animator animator, string paramName)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
                if (param.name == paramName)
                    return true;
            return false;
        }

        public static void SetFloatSafe(this Animator animator, string paramName, float value)
        {
            if (animator.HasParameter(paramName))
                animator.SetFloat(paramName, value);
        }

        public static void SetIntegerSafe(this Animator animator, string paramName, int value)
        {
            if (animator.HasParameter(paramName))
                animator.SetInteger(paramName, value);
        }

        public static void SetBoolSafe(this Animator animator, string paramName, bool value)
        {
            if (animator.HasParameter(paramName))
                animator.SetBool(paramName, value);
        }

        public static bool CheckName(this AnimatorStateInfo info, ICollection<string> stateNames)
        {
            foreach (string str in stateNames)
                if (info.IsName(str))
                    return true;
            return false;
        }

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

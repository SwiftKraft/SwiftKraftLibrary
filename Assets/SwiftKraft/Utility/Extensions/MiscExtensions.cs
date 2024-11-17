using UnityEngine;

namespace SwiftKraft.Utils
{
    public static class MiscExtensions
    {
        public static bool TryGetComponentInChildren<T>(this Component comp, out T component) where T : Component
        {
            component = comp.GetComponentInChildren<T>();
            return component != null;
        }

        public static bool TryGetComponentInParent<T>(this Component comp, out T component) where T : Component
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
    }
}

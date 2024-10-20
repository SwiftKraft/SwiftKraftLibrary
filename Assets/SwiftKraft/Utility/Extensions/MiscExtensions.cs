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
    }
}

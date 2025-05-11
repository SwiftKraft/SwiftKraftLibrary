using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftKraft.Utils
{
    public static class ComponentExtensions
    {
        public static void CleanDestroy(this Component target)
        {
            if (target == null) return;

            GameObject go = target.gameObject;
            Type targetType = target.GetType();

            var toDestroy = go.GetComponents<Component>()
                .Where(comp => comp != null && comp != target)
                .Where(comp =>
                {
                    var requireAttributes = comp.GetType()
                        .GetCustomAttributes(typeof(RequireComponent), true)
                        .OfType<RequireComponent>();

                    return requireAttributes.Any(attr =>
                        (attr.m_Type0 != null && attr.m_Type0.IsAssignableFrom(targetType)) ||
                        (attr.m_Type1 != null && attr.m_Type1.IsAssignableFrom(targetType)) ||
                        (attr.m_Type2 != null && attr.m_Type2.IsAssignableFrom(targetType))
                    );
                })
                .ToList();

            foreach (var comp in toDestroy)
                comp.CleanDestroy();

            Object.Destroy(target);
        }
    }
}

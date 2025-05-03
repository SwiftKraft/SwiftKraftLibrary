
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftKraft.Utils
{
    public static class ComponentExtensions
    {
        public static void DestroyRequiredComponents(this Component monoInstanceCaller)
        {
            MemberInfo memberInfo = monoInstanceCaller.GetType();
            RequireComponent[] requiredComponentsAtts = Attribute.GetCustomAttributes(memberInfo, typeof(RequireComponent), true) as RequireComponent[];
            foreach (RequireComponent rc in requiredComponentsAtts)
            {
                if (rc != null && monoInstanceCaller.GetComponent(rc.m_Type0) != null)
                {
                    Object.Destroy(monoInstanceCaller.GetComponent(rc.m_Type0));
                }
            }
        }
    }
}

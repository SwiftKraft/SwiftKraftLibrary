// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System;
    using System.Linq;
    using UnityEngine;
    using UnityEditor;
    using Interpolation;
    using Coherence.Toolkit;
    using Object = UnityEngine.Object;

    [CustomPropertyDrawer(typeof(InterpolationPickerAttribute))]
    internal class InterpolationPickerDrawer : ObjectPickerDrawer
    {
        protected override GUIContent GetIconContent(SerializedProperty property)
        {
            var obj = property.objectReferenceValue as InterpolationSettings;
            return ContentUtils.GetInterpolationContent(obj);
        }

        protected override Object CreateInstance()
        {
            return InterpolationSettings.CreateDefault();
        }

        protected override void OnReferenceChanged(Object oldReference, Object newReference)
        {
            // On this method, we want to know if interpolation setting has changed from/to None.
            // We do this to dirty the schemas / require a bake.
            // We only care about it happening on CoherenceSync components (drawer could be used anywhere else).

            if (!objs.Any(o => o is CoherenceSync))
            {
                return;
            }

            if (!path.StartsWith("bindings.Array", StringComparison.InvariantCulture))
            {
                return;
            }

            var oldIsNone = (oldReference is InterpolationSettings { IsInterpolationNone: true } || !oldReference);
            var newIsNone = (newReference is InterpolationSettings { IsInterpolationNone: true } || !newReference);

            if (oldIsNone != newIsNone)
            {
                BakeUtil.CoherenceSyncSchemasDirty = true;
            }
        }
    }
}

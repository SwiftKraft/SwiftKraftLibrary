// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(PrefabSyncGroup))]
    internal class PrefabSyncGroupEditor : BaseEditor
    {
        private List<CoherenceSync> coherenceSyncTargets = new List<CoherenceSync>();

        protected override void OnEnable()
        {
            base.OnEnable();
            coherenceSyncTargets.Clear();
            var comp = (target as Component);
            var syncs = comp.GetComponentsInChildren<CoherenceSync>(true);

            foreach (var sync in syncs)
            {
                if (sync.transform.parent != comp.transform.parent)
                {
                    coherenceSyncTargets.Add(sync);
                }
            }
        }

        protected override void OnGUI()
        {
            CoherenceHubLayout.DrawMessageArea(
                $"{nameof(PrefabSyncGroup)} will take care of rebuilding the Prefab hierarchy of networked children in remote Clients." +
                "\n\nThis Component is needed to instantiate the Prefab in runtime, for Prefab instances saved in a Scene, you can disable this Component and sync the hierarchy normally using Uniqueness.");

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawBoldLabel(new GUIContent("Tracked Child CoherenceSyncs"));
                GUILayout.FlexibleSpace();
                CoherenceHubLayout.DrawBoldLabel(new GUIContent($"{coherenceSyncTargets.Count}"));
            }

            EditorGUILayout.Separator();

            using var disabled = new EditorGUI.DisabledScope(true);
            foreach (var sync in coherenceSyncTargets)
            {
                EditorGUILayout.ObjectField(sync, typeof(CoherenceSync), false);
            }
        }
    }
}

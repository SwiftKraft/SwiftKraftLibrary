// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using UnityEditor.AssetImporters;
    using UnityEngine;

    [CanEditMultipleObjects, CustomEditor(typeof(SchemaImporter))]
    internal class SchemaImporterEditor : ScriptedImporterEditor
    {
        private SchemaAsset schemaAsset;

        protected override bool needsApplyRevert => false;

        public override void OnEnable()
        {
            base.OnEnable();

            if (assetTarget is not SchemaAsset asset)
            {
                return;
            }

            asset.hideFlags &= ~HideFlags.NotEditable;
            schemaAsset = asset;
        }

        public override void OnInspectorGUI()
        {
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEditor.AssetImporters;
    using UnityEngine;

    [ScriptedImporter(3, Constants.schemaExtension)]
    internal sealed class SchemaImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            // GUID of the SchemaAsset script (as found in SchemaAsset.cs.meta).
            // Using GUID instead of path for resilience.
            // This importer is dependant on SchemaAsset being imported and properly loaded into memory.
            // Without this dependency set, there can be scenarios where this importer triggers and tries to import
            // a .schema file without Unity having imported and loaded the SchemaAsset ScriptableObject.
            // A way this can manifest is by AddObjectToAsset (see below) throwing an InvalidOperationException.
            context.DependsOnSourceAsset(new GUID("583bf8a6352b543cf896bb83930ccd6e"));

            var asset = ScriptableObject.CreateInstance<SchemaAsset>();

            try
            {
                asset.raw = File.ReadAllText(context.assetPath, Encoding.UTF8);
                asset.identifier = Path.GetFileName(context.assetPath);
                asset.SchemaDefinition = SchemaReader.Read(asset.raw);
            }
            catch (Exception e)
            {
                context.LogImportError(e.ToString(), asset);
            }
            context.AddObjectToAsset(Constants.schemaAssetIdentifier, asset);
            context.SetMainObject(asset);
        }
    }
}

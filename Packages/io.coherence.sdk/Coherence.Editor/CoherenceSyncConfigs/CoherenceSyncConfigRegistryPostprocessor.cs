namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEditor.Callbacks;

    internal class CoherenceSyncConfigRegistryPostprocessor : AssetPostprocessor
    {
        [RunAfterClass(typeof(Postprocessor))]
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (CloneMode.Enabled)
            {
                return;
            }

            foreach (var importedAsset in importedAssets)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(importedAsset) != typeof(CoherenceSyncConfigRegistry))
                {
                    continue;
                }

                var registry = AssetDatabase.LoadAssetAtPath<CoherenceSyncConfigRegistry>(importedAsset);
                registry.ReimportConfigs();
            }
        }
    }
}


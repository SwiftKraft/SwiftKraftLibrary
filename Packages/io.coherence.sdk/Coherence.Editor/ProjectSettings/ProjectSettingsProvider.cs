// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;

    internal static class ProjectSettingsProvider
    {
        [SettingsProvider]
        private static SettingsProvider CreateProvider()
        {
            var keywords = SettingsProvider.GetSearchKeywordsFromSerializedObject(new SerializedObject(ProjectSettings.instance));
            var provider = AssetSettingsProvider.CreateProviderFromObject(Paths.projectSettingsWindowPath, ProjectSettings.instance, keywords);
            return provider;
        }
    }
}

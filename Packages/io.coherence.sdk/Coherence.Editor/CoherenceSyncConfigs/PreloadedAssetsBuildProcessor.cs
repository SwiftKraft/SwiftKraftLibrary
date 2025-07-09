// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Editor
{
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    internal class PreloadedAssetsBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1001;

        public void OnPreprocessBuild(BuildReport report)
        {
            Postprocessor.EnsurePreloadedAssets();
        }
    }
}


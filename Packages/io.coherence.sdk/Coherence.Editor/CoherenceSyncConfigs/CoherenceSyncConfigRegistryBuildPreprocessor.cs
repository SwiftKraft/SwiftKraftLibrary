// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEditor.Compilation;

    internal class CoherenceSyncConfigRegistryBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        private object compilationContext;

        private static void OnBeforeBuild()
        {
            CoherenceSyncConfigRegistry.Instance.Store();
            SaveRegistry();
        }

        private static void OnAfterBuild()
        {
            CoherenceSyncConfigRegistry.Instance.ClearStore();
            SaveRegistry();
        }

        private static void SaveRegistry()
        {
            AssetDatabase.SaveAssetIfDirty(CoherenceSyncConfigRegistry.Instance);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            OnBeforeBuild();

            compilationContext = null;

            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        private void OnCompilationStarted(object ctx)
        {
            compilationContext = ctx;
        }

        private void OnCompilationFinished(object ctx)
        {
            if (compilationContext != ctx)
            {
                return;
            }

            compilationContext = null;

            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;

            EditorApplication.delayCall += OnAfterBuild;
        }
    }
}

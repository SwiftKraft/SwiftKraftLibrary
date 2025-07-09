// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.CodeGen
{
    using UnityEditor;
    using UnityEngine;
    using Logger = Log.Logger;

    public class CodeGenRunnerUnityEnvironment : CodeGenRunnerEnvironment
    {
        private readonly Logger logger;

        public CodeGenRunnerUnityEnvironment(Logger logger)
        {
            this.logger = logger;
        }

        public override bool IsReady()
        {
            if (!EditorApplication.isCompiling)
            {
                return true;
            }

            logger.Warning(Log.Warning.CodeGenWarning, "Cannot generate code while compiling. Aborting.");
            return false;
        }

        protected override void PrepareCodeGen()
        {
            LockUnityState();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayProgressBar("Code Generation", "Generating network code..", 1f);
            }
        }

        protected override void FinalizeCodeGen()
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.ClearProgressBar();
            }

            RefreshAndUnlockUnity();
        }

        private static void RefreshAndUnlockUnity()
        {
            AssetDatabase.AllowAutoRefresh();
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.Refresh();
        }

        private static void LockUnityState()
        {
            AssetDatabase.DisallowAutoRefresh();
            EditorApplication.LockReloadAssemblies();
        }
    }
}

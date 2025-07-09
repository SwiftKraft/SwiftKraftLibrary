// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Log;
    using UnityEditor;
    using UnityEditor.Build;

    internal class DefinesManager : IActiveBuildTargetChanged
    {
        public int callbackOrder => 1;
        private const string SkipLongUnitTestsDefine = "COHERENCE_SKIP_LONG_UNIT_TESTS";

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            ApplyCorrectLogLevelDefines(Log.GetSettings().LogLevel);
        }

        public static void ApplyCorrectLogLevelDefines(LogLevel selectedLevel)
        {
            var namedBuildTarget = GetActiveNamedBuildTarget();

            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] defines);

            ApplyLogLevelDefine(selectedLevel, LogLevel.Debug, ref defines, LogConditionals.Debug);
            ApplyLogLevelDefine(selectedLevel, LogLevel.Trace, ref defines, LogConditionals.Trace);

            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
        }

        public static void ApplySkipLongUnitTestsDefine(bool skipEnabled)
        {
            var namedBuildTarget = GetActiveNamedBuildTarget();

            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] defines);
            ApplyDefine(skipEnabled, ref defines, SkipLongUnitTestsDefine);
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
        }

        public static bool IsSkipLongTestsDefineEnabled()
        {
            var namedBuildTarget = GetActiveNamedBuildTarget();
            PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] defines);
            return ArrayUtility.Contains(defines, SkipLongUnitTestsDefine);
        }

        private static void ApplyLogLevelDefine(LogLevel selectedLevel, LogLevel forLevel, ref string[] defines, string define)
        {
            ApplyDefine(selectedLevel <= forLevel, ref defines, define);
        }

        private static void ApplyDefine(bool exists, ref string[] defines, string define)
        {
            if (exists)
            {
                if (!ArrayUtility.Contains(defines, define))
                {
                    ArrayUtility.Add(ref defines, define);
                }
            }
            else
            {
                ArrayUtility.Remove(ref defines, define);
            }
        }

        // Taken from https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/BuildPipeline/NamedBuildTarget.cs#L133
        private static NamedBuildTarget GetActiveNamedBuildTarget()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            if (buildTargetGroup == BuildTargetGroup.Standalone && EditorUserBuildSettings.standaloneBuildSubtarget == StandaloneBuildSubtarget.Server)
            {
                return NamedBuildTarget.Server;
            }

            return NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        }
    }
}

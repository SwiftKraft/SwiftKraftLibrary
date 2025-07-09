// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Build
{
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    public class SimulatorBuildSteps : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var options = SimulatorBuildOptions.Get();

            if (!SimulatorBuildPipeline.IsBuildingSimulator)
            {
                return;
            }

            options.BuildSizeOptimizations.Optimize();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var options = SimulatorBuildOptions.Get();

            if (!SimulatorBuildPipeline.IsBuildingSimulator)
            {
                return;
            }

            options.BuildSizeOptimizations.Restore();
        }
    }
}

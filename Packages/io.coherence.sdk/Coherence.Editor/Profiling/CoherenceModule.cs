// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Profiling
{
    using Coherence.Toolkit.Profiling;
    using Unity.Profiling;
    using Unity.Profiling.Editor;

    [System.Serializable]
    [ProfilerModuleMetadata("coherence", IconPath = "Packages/io.coherence.sdk/Coherence.Editor/Icons/EditorWindow.png")]
    public class CoherenceModule : ProfilerModule
    {
        private static ProfilerCategory category = Counters.Category;

        private static readonly ProfilerCounterDescriptor[] ChartCounters =
        {
            new ProfilerCounterDescriptor("Bandwidth In", category),
            new ProfilerCounterDescriptor("Updates In", category),
            new ProfilerCounterDescriptor("Commands In", category),
            new ProfilerCounterDescriptor("Inputs In", category),
            new ProfilerCounterDescriptor("Bandwidth Out", category),
            new ProfilerCounterDescriptor("Updates Out", category),
            new ProfilerCounterDescriptor("Commands Out", category),
            new ProfilerCounterDescriptor("Inputs Out", category),
            new ProfilerCounterDescriptor("Latency", category),
            new ProfilerCounterDescriptor("Entities", category),
        };

        private static readonly string[] AutoEnabledCategoryNames =
        {
            category.Name,
        };

        public CoherenceModule() : base(ChartCounters, autoEnabledCategoryNames: AutoEnabledCategoryNames)
        {
        }
    }
}

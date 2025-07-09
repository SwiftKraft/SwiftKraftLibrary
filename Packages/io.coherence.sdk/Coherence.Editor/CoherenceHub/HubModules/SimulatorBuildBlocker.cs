// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;

    [Flags]
    internal enum SimulatorBuildBlocker
    {
        None = 0,
        NoSimulatorSlugProvided = 1,
        NoScenesInBuildOptions = 2,
        LinuxDedicatedServerBuildSupportInstalled = 4,
        LinuxBuildSupportInstalled = 8,
        LinuxInstalled = LinuxDedicatedServerBuildSupportInstalled | LinuxBuildSupportInstalled,
        EditorImportingOrCompiling = 16,
        HasNoScenes = 32,
        CoherenceSimulatorDirectiveMissing = 64,
        CannotUploadLinuxBuild = 128,
        OrganizationIdMissing = 256,
        ProjectIdMissing = 512,
    }
}

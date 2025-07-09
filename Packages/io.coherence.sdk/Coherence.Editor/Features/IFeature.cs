// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Features
{
    /// <summary>
    /// Feature flags for Coherence.
    /// </summary>
    internal interface IFeature
    {
        /// <summary>
        /// Returns <see langword="true"/> if the feature is enabled, <see langword="false" /> otherwise.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Returns <see langword="true"/> if the feature can be enabled by the user, <see langword="false" /> otherwise.
        /// </summary>
        bool IsUserConfigurable { get; }
    }
}

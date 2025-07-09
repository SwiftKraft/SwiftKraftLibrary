// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Features
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    internal static class FeaturesManager
    {
        private static readonly Dictionary<string, IFeature> Features = new();

        internal static IFeature GetFeature([DisallowNull] string featureName) =>
            Features.GetValueOrDefault(featureName);

        static FeaturesManager()
        {
            // Add features here
            Features.Add(FeatureFlags.MultiRoomSimulator, new MultiRoomSimulator());
        }
    }
}

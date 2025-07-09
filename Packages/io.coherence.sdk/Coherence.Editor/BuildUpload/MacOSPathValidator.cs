// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    internal class MacOSPathValidator : BuildPathValidator
    {
        internal override string GetInfoString()
        {
            return "Select a valid macOS Application Bundle of your game (.app)";
        }

        protected override bool ValidateInternal(string buildPath)
        {
            return buildPath.EndsWith(".app");
        }
    }
}

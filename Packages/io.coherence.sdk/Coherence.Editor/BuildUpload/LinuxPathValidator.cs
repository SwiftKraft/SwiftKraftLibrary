// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.IO;
    using System.Linq;

    internal class LinuxPathValidator : BuildPathValidator
    {
        internal override string GetInfoString()
        {
            return "Select a folder that includes a valid Linux build with a UnityPlayer.so file.";
        }
        
        protected override bool ValidateInternal(string buildPath)
        {
            return Directory.EnumerateFiles(buildPath).Any(file => file.EndsWith("UnityPlayer.so"));
        }
    }
}

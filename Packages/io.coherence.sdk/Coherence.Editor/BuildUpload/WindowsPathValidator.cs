// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.IO;
    
    internal class WindowsPathValidator : BuildPathValidator
    {
        internal override string GetInfoString()
        {
            return "Select a folder that includes a valid Windows build with a .exe and UnityPlayer.dll files.";
        }

        protected override bool ValidateInternal(string buildPath)
        {
            bool validatedExe = false;
            bool validatedUnityPlayerDll = false;
            
            foreach (var file in Directory.EnumerateFiles(buildPath))
            {
                if (file.EndsWith(".exe"))
                {
                    validatedExe = true;
                }

                if (file.EndsWith("UnityPlayer.dll"))
                {
                    validatedUnityPlayerDll = true;
                }
            }

            return validatedExe && validatedUnityPlayerDll;
        }
    }
}

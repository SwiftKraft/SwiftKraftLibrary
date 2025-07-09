// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Collections.Generic;
    using System.IO;

    internal class WebGLPathValidator : BuildPathValidator
    {
        private const string buildFolder = "/Build";

        private readonly List<string> filesToValidate = new List<string>()
        {
            ".loader.js",
            ".framework.js",
            ".wasm",
            ".data"
        };
        
        internal override string GetInfoString()
        {
            return "Select a folder that includes a valid WebGL build. Must have the Build and, optionally, StreamingAssets folders.";
        }
        
        protected override bool ValidateInternal(string buildPath)
        {
            var completeBuildFolder = $"{buildPath}{buildFolder}";

            if (!Directory.Exists(completeBuildFolder))
            {
                return false;
            }

            int filesValidated = 0;

            foreach (var fileExtension in filesToValidate)
            {
                foreach (var file in Directory.EnumerateFiles(completeBuildFolder))
                {
                    if (!file.Contains(fileExtension))
                    {
                        continue;
                    }

                    filesValidated++;
                    break;
                }
            }

            return filesValidated == filesToValidate.Count;
        }
    }
}

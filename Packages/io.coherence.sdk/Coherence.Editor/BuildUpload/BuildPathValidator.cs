// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using UnityEngine;

    internal abstract class BuildPathValidator
    {
        internal bool Validate(string buildPath)
        {
            try
            {
                if (!Directory.Exists(buildPath))
                {
                    return false;
                }

                return ValidateInternal(buildPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
        
        internal abstract string GetInfoString();
        protected abstract bool ValidateInternal(string buildPath);
    }
}

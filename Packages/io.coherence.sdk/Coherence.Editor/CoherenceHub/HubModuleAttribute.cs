// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;

    public class HubModuleAttribute : Attribute
    {
        /// <summary>
        /// Decides tab position in CoherenceHub (Overview is 100, so other windows should have lesser value)
        /// </summary>
        public int Priority
        {
            get;
            set;
        }
    }
}

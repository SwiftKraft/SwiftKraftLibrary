// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;

    public class ReadOnlyAttribute : PropertyAttribute
    {
        public bool IncludeChildren { get; set; }
        
        public ReadOnlyAttribute(bool includeChildren)
        {
            IncludeChildren = includeChildren;
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DisplayNameAttribute : Attribute
    {
        public string Name { get; }
        public string Tooltip { get; }

        public DisplayNameAttribute(string name, string tooltip)
        {
            Name = name;
            Tooltip = tooltip;
        }
    }
}


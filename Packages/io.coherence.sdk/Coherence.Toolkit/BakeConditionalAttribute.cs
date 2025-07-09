// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;

    /// <summary>
    /// Conditionally compile the baked code for this type, effectively disabling the
    /// network synchronization for this type if the condition is not met.
    /// </summary>
    /// <remarks>The condition is directly appended to the #if directive (see example)</remarks>
    /// <example>[BakeConditional("DEBUG && CHEATS_ENABLED")]</example>
    [AttributeUsage(AttributeTargets.Class)]
    public class BakeConditionalAttribute : Attribute
    {
        public string Condition;

        public BakeConditionalAttribute(string condition)
        {
            Condition = condition;
        }
    }
}

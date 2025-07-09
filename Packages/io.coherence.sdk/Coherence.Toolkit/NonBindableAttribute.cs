// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Attribute to skip types from being considered bindable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subtypes are also considered non-bindable.
    /// </para>
    /// <para>
    /// Tag classes with this attribute to avoid them from showing up on the configure window, or be added as <see cref="CoherenceSync.Bindings"/>.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class NonBindableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="NonBindableAttribute"/>.
        /// </summary>
        public NonBindableAttribute()
        {

        }
    }
}

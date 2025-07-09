// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;

    /// <summary>
    ///     Specifies a method to be called when the value of a synced field/property changes. This method will be called only
    ///     for instances whose <see cref="CoherenceSync.HasStateAuthority"/> evaluates to false.
    ///     <see cref="CoherenceSync.isSimulated" />).
    /// </summary>
    /// <remarks>
    ///     The callback method must meet the following conditions:
    ///     <para>* Be 'public'</para>
    ///     <para>* Be defined in the same component as the field/property</para>
    ///     <para>* Have exactly two parameters</para>
    ///     <para>* Type of both parameters must match the type of field/property on which this attribute was applied</para>
    ///     <para>
    ///         Example of a method signature for a field of type 'int': 'public void OnMyValueSynced(int oldValue, int
    ///         newValue)'
    ///     </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OnValueSyncedAttribute : Attribute
    {
        /// <summary>
        ///     Name of the method to be called when value changes. We strongly recommend using the 'nameof()' operator to avoid
        ///     typing errors (e.g. '[<see cref="OnValueSyncedAttribute" />(nameof(OnMyValueSynced)]' ).
        /// </summary>
        public readonly string Callback;

        /// <summary>
        ///     If true, fields (or properties) marked with this attribute, but not marked as synced, won't generate any error
        ///     logs.
        /// </summary>
        public bool SuppressNotBoundError { get; set; }

        /// <summary>If true, callbacks with parameter names suggesting invalid order won't generate any error logs.</summary>
        public bool SuppressParamOrderError { get; set; }

        public OnValueSyncedAttribute(string callback)
        {
            Callback = callback;
        }
    }
}

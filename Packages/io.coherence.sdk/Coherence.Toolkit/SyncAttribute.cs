// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using Bindings;

    /// <summary>
    /// Automatically add this member variable or property
    /// to the list of synced bindings in CoherenceSync.
    ///
    /// Variables and properties without this attribute can still
    /// be synced by selecting them manually in the bindings configuration window.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SyncAttribute : Attribute
    {
        /// <summary>
        /// Used for migration purposes. Bindings whose name match OldName will be updated to the new target member.
        /// </summary>
        public string OldName { get; }

        /// <summary>
        /// Sets the sync mode on the binding.
        /// </summary>
        public SyncMode DefaultSyncMode = SyncMode.Always;

        public SyncAttribute()
        {
        }

        public SyncAttribute(string oldName = null) => OldName = oldName;
    }
}

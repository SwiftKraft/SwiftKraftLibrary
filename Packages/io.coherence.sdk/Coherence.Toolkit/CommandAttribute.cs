// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;

    /// <summary>
    /// Automatically add this method to the list of synced
    /// bindings in CoherenceSync. This makes the method available
    /// as a target for SendCommand.
    ///
    /// Methods without this attribute can still be marked as viable
    /// targets for SendCommand by selecting them manually in the
    /// bindings configuration window.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandAttribute : Attribute
    {
        /// <summary>
        /// Sets the default routing on the binding (it can still
        /// be changed in the bindings configuration window.)
        /// </summary>
        public MessageTarget defaultRouting = MessageTarget.All;

        /// <summary>
        /// Used for migration purposes. Method bindings whose name match OldName will be updated to the new target member.
        /// </summary>
        public string OldName { get; }

        /// <summary>
        /// Used for migration purposes. Method bindings whose parameters match OldParams will be updated to the new target member.
        /// </summary>
        public Type[] OldParams { get; }

        public CommandAttribute()
        {
        }

        public CommandAttribute(string oldName = null, params Type[] oldParams)
        {
            OldName = oldName;
            OldParams = oldParams;
        }
    }
}

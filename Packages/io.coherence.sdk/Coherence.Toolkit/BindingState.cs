// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    /// <summary>
    /// Binding status of a member method.
    /// </summary>
    internal enum BindingState
    {
        /// <summary>
        /// Binding is valid and can be used as a network command.
        /// </summary>
        Valid,

        /// <summary>
        /// The type is not supported and no methods can be used as a network command.
        /// </summary>
        UnsuppotedType,

        /// <summary>
        /// One or more parameters are invalid, or the method returns a value.
        /// </summary>
        Incompatible,

        /// <summary>
        /// The method is marked as obsolete. It can still be used as a network command.
        /// </summary>
        Obsolete,

        /// <summary>
        /// A <see cref="CommandAttribute"/> has been placed on a getter or setter, or add/remove for an Event.
        /// </summary>
        SpecialName
    }
}

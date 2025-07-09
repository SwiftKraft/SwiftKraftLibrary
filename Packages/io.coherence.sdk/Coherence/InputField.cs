namespace Coherence
{
    using System;

    /// <summary>
    ///     An input definition, e.g. a single button.
    ///     A `CoherenceInput` can have a collection of these.
    /// </summary>
    [Serializable]
    public struct Field
    {
        /// <summary>
        ///     The type of the input.
        /// </summary>
        public enum Type
        {
            /// <summary>
            ///     Joystick or gamepad axis, uses a 2-dimensional vector.
            /// </summary>
            Axis2D,
    
            /// <summary>
            ///     A button that can be either pressed or not.
            /// </summary>
            Button,
    
            /// <summary>
            ///     A single scalar value, "how hard the button is pressed".
            /// </summary>
            Axis,
    
            /// <summary>
            ///     Any arbitrary string value.
            /// </summary>
            String,
    
            /// <summary>
            ///     3-dimensional vector, used for example for VR headset position.
            /// </summary>
            Axis3D,
    
            /// <summary>
            ///     Quaternion (rotation), used for example for VR headset rotation.
            /// </summary>
            Rotation,
    
            /// <summary>
            ///     Integer, used for example for deterministic input.
            /// </summary>
            Integer,
        }
    
        /// <summary>
        ///     The name of the input, e.g. "Jump".
        /// </summary>
        public string name;
    
        /// <summary>
        ///     The type of the input.
        /// </summary>
        public Type type;
    }
}

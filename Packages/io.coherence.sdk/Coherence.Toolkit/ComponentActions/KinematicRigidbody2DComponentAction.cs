// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.ComponentActions
{
    using System;
    using UnityEngine;
    using UnityEngine.Serialization;

    [ComponentAction(typeof(Rigidbody2D), "Is Kinematic")]
    public sealed class KinematicRigidbody2DComponentAction : ComponentAction
    {
        [Tooltip("When there's no authority over this entity, set 'Is Kinematic' to enabled.")]
        [SerializeField, FormerlySerializedAs("setOnRemote")]
        private bool enableOnRemote = true;

        [Tooltip("When there's authority over this entity, set 'Is Kinematic' to disabled.")]
        [SerializeField, FormerlySerializedAs("resetOnAuthority")]
        private bool disableOnAuthority = true;

        [Obsolete("Access to this member will be removed in a future version.")]
        [Deprecated("07/2024", 1, 2, 4, Reason = "Field was renamed for clarity and made private to improve encapsulation.")]
        public bool setOnRemote
        {
            get => enableOnRemote;
            set => enableOnRemote = value;
        }

        [Obsolete("Access to this member will be removed in a future version.")]
        [Deprecated("07/2024", 1, 2, 4, Reason = "Field was renamed for clarity and made private to improve encapsulation.")]
        public bool resetOnAuthority
        {
            get => disableOnAuthority;
            set => disableOnAuthority = value;
        }

        public override void OnAuthority()
        {
            if (!disableOnAuthority)
            {
                return;
            }

            if (component is not Rigidbody2D rb2d)
            {
                return;
            }

            rb2d.bodyType = RigidbodyType2D.Dynamic;
        }

        public override void OnRemote()
        {
            if (!enableOnRemote)
            {
                return;
            }

            if (component is not Rigidbody2D rb2d)
            {
                return;
            }

            rb2d.bodyType = RigidbodyType2D.Kinematic;
        }
    }
}

using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Damagables
{
    public abstract class Hitbox : MonoBehaviour
    {
        public virtual bool Active { get; set; }

        public TransformDataScale PreviousTransformData { get; protected set; }

        protected virtual void FixedUpdate()
        {
            if (!Active)
                return;

            PhysicsCast();

            PreviousTransformData = new(transform);
        }

        public abstract void PhysicsCast();

        public abstract void Hit(Transform target, Vector3 point);
    }
}

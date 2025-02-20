using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class MultiModifyTransform : MonoBehaviour
    {
        public Vector3 OriginalPosition { get; set; }
        public Quaternion OriginalRotation { get; set; }

        public class Modifier
        {
            public readonly MultiModifyTransform Parent;

            public Modifier(MultiModifyTransform parent) => Parent = parent;

            private Vector3 position = Vector3.zero;
            private Quaternion rotation = new(0f, 0f, 0f, 1f);

            public Vector3 Position
            {
                get => position;
                set
                {
                    if (position == value)
                        return;

                    position = value;
                    Parent.UpdateTransform();
                }
            }
            public Quaternion Rotation
            {
                get => rotation;
                set
                {
                    if (rotation == value)
                        return;

                    rotation = value;
                    Parent.UpdateTransform();
                }
            }
        }

        public readonly List<Modifier> Modifiers = new();

        private void Awake()
        {
            OriginalPosition = transform.localPosition;
            OriginalRotation = transform.localRotation;
        }

        public void UpdateTransform()
        {
            transform.SetLocalPositionAndRotation(OriginalPosition, OriginalRotation);
            foreach (Modifier modifier in Modifiers)
                transform.SetLocalPositionAndRotation(transform.localPosition + modifier.Position, 
                    transform.localRotation * modifier.Rotation);
        }

        public Modifier AddModifier()
        {
            Modifier n = new(this);
            Modifiers.Add(n);
            return n;
        }

        public void RemoveModifier(Modifier mod) => Modifiers.Remove(mod);
    }
}

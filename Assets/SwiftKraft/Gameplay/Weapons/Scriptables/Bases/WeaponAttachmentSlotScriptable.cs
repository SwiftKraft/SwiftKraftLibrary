using SwiftKraft.Gameplay.Interfaces;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Weapons/Attachment Slot", fileName = "New Attachment Slot", order = 1)]
    public class WeaponAttachmentSlotScriptable : ScriptableObject
    {
        public string SlotName;

        public Attachment[] Attachments;

#if UNITY_EDITOR
        public void DrawGizmos(Transform root)
        {
            for (int i = 0; i < Attachments.Length; i++)
                Attachments[i]?.DrawGizmos(root);
        }
#endif

        [Serializable]
        public class Attachment : ICloneable<Attachment>
        {
            public string name;

            [HideInInspector]
            public WeaponAttachmentSlot parent;

            [SerializeReference, Subclass]
            public AttachmentProperty[] properties;

            public void Init(WeaponAttachmentSlot parent)
            {
                this.parent = parent;
                foreach (AttachmentProperty prop in properties)
                    prop?.Init(this);
            }

            public void Update()
            {
                foreach (AttachmentProperty prop in properties)
                    prop?.Update();
            }

            public void Uninstall()
            {
                foreach (AttachmentProperty prop in properties)
                    prop?.Uninstall();
            }

            public void Destroy()
            {
                foreach (AttachmentProperty prop in properties)
                    prop?.Destroy();
            }

#if UNITY_EDITOR
            public void DrawGizmos(Transform root)
            {
                for (int i = 0; i < properties.Length; i++)
                    properties[i]?.DrawGizmos(root);
            }
#endif

            public Attachment Clone()
            {
                Attachment clone = new()
                {
                    parent = parent,
                    name = name,
                    properties = new AttachmentProperty[properties.Length]
                };

                for (int i = 0; i < clone.properties.Length; i++)
                    clone.properties[i] = properties[i].Clone();

                return clone;
            }
        }

        [Serializable]
        public abstract class AttachmentProperty : ICloneable<AttachmentProperty>
        {
            [HideInInspector]
            public Attachment parent;

            public virtual void Init(Attachment parent) => this.parent = parent;

            public abstract void Update();

            public virtual void Uninstall() { }

            public virtual void Destroy() { }

            public virtual void DrawGizmos(Transform root) { }

            public abstract AttachmentProperty Clone();
        }
    }
}

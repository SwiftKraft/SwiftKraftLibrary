using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class AttachmentComponentPropertyBase<T> : WeaponAttachmentSlot.AttachmentProperty where T : Component
    {
        public T Component { get; protected set; }

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            Component = this.parent.parent.GetComponentInParent<T>();
        }
    }
}

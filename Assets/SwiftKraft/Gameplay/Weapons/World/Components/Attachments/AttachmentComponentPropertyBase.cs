using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class AttachmentComponentPropertyBase<T> : WeaponAttachmentSlotScriptable.AttachmentProperty where T : Component
    {
        public T Component { get; protected set; }

        public override void Init(WeaponAttachmentSlotScriptable.Attachment parent)
        {
            base.Init(parent);
            Component = this.parent.parent.GetComponentInParent<T>();
        }
    }
}

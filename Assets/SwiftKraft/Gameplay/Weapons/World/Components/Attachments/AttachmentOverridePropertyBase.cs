using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class AttachmentOverridePropertyBase<T, O> : AttachmentComponentPropertyBase<T> where T : Component where O : OverrideBase
    {
        protected O overrider;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            overrider = CreateOverrider();
        }

        public abstract O CreateOverrider();

        public abstract void ApplyOverrides();

        public override void Update()
        {
            if (overrider != null)
                ApplyOverrides();
        }

        public override void Destroy()
        {
            base.Destroy();
            overrider.Dispose();
        }
    }
}

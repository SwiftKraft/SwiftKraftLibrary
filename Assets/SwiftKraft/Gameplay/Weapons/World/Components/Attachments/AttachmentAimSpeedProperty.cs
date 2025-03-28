using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAimSpeedProperty : AttachmentComponentPropertyBase<WeaponAds>
    {
        public float value;
        public ModifiableStatistic.ModifierType type;

        ModifiableStatistic.Modifier modifier;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            modifier = Component.AimSpeedMultiplier.AddModifier();
        }

        public override void Update()
        {
            if (modifier != null)
            {
                modifier.Value = value;
                modifier.Type = type;
            }
        }

        public override void Uninstall()
        {
            base.Uninstall();
            modifier.Value = 1f;
            modifier.Type = ModifiableStatistic.ModifierType.Multiplication;
        }

        public override void Destroy()
        {
            base.Destroy();
            modifier.Dispose();
        }
    }
}

using SwiftKraft.Gameplay.Weapons.Triggers;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentPropertyOverrideTriggerAction : AttachmentComponentPropertyBase<WeaponTrigger>
    {
        public WeaponTrigger.Action[] Overrides;

        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentPropertyOverrideTriggerAction()
            {
                Overrides = (WeaponTrigger.Action[])Overrides.Clone()
            };

        public override void Update()
        {
            for (int i = 0; i < Overrides.Length; i++)
                Component.CreateOverride(Overrides[i]);
        }

        public override void Uninstall()
        {
            base.Uninstall();
            for (int i = 0; i < Overrides.Length; i++)
                Component.RemoveOverride(Overrides[i]);
        }
    }
}

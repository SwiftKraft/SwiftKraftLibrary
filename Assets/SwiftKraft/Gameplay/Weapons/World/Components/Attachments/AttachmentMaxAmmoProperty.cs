using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentMaxAmmoProperty : AttachmentStatisticPropertyBase<WeaponAmmo>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentMaxAmmoProperty()
            {
                value = value,
                type = type,
            };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.MaxAmmo.AddModifier();
    }
}

using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentPropertyAttackCooldown : AttachmentStatisticPropertyBase<WeaponBase>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentPropertyAttackCooldown()
            {
                value = value,
                type = type,
            };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.CurrentMode is WeaponAttackCooldown cooldown ? cooldown.CooldownDelay.AddModifier() : null;
    }

    public class AttachmentPropertyAttackPrefire : AttachmentStatisticPropertyBase<WeaponBase>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentPropertyAttackPrefire()
            {
                value = value,
                type = type,
            };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.CurrentMode is WeaponAttackCooldown cooldown ? cooldown.PrefireDelay.AddModifier() : null;
    }
}
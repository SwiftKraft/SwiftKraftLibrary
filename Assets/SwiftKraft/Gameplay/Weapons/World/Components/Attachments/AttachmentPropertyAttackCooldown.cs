using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentPropertyAttackCooldown : AttachmentStatisticPropertyBase<WeaponDelay>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentPropertyAttackCooldown()
            {
                value = value,
                type = type,
            };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.CooldownDelay.AddModifier();
    }

    public class AttachmentPropertyAttackPrefire : AttachmentStatisticPropertyBase<WeaponDelay>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentPropertyAttackPrefire()
            {
                value = value,
                type = type,
            };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.PrefireDelay.AddModifier();
    }
}
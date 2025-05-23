using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAttackAmountProperty : AttachmentAttackStatisticPropertyBase<WeaponAttackBase>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() => new AttachmentAttackAmountProperty()
        {
            type = type,
            value = value,
            AttackModeIndex = AttackModeIndex,
        };

        public override ModifiableStatistic.Modifier CreateOverrider() => Attacker.Amount.AddModifier();
    }
}

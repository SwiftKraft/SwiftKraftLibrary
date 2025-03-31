using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAimSpreadProperty : AttachmentStatisticPropertyBase<WeaponSpread>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentAimSpreadProperty()
            {
                value = value,
                type = type,
            };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.AimMultiplier.AddModifier();
    }
}

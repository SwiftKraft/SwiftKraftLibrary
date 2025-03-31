using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentHipSpreadProperty : AttachmentStatisticPropertyBase<WeaponSpread>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentHipSpreadProperty()
            {
                value = value,
                type = type,
            };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.HipMultiplier.AddModifier();
    }
}

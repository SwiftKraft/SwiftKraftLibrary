using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAimSpeedProperty : AttachmentStatisticPropertyBase<WeaponAds>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentAimSpeedProperty()
            {
                value = value,
                type = type,
            };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.AimSpeedMultiplier.AddModifier();
    }
}

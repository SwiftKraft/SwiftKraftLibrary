using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAimSpeedProperty : AttachmentStatisticPropertyBase<WeaponAds>
    {
        public override ModifiableStatistic.Modifier CreateOverrider() => Component.AimSpeedMultiplier.AddModifier();
    }
}

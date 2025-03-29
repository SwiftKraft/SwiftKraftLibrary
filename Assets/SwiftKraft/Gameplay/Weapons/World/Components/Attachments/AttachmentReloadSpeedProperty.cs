using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentReloadSpeedProperty : AttachmentStatisticPropertyBase<WeaponAmmo>
    {
        public override ModifiableStatistic.Modifier CreateOverrider() => Component.ReloadSpeedMultiplier.AddModifier();
    }
}

using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentHipSpreadProperty : AttachmentStatisticPropertyBase<WeaponSpread>
    {
        public override ModifiableStatistic.Modifier CreateOverrider() => Component.HipMultiplier.AddModifier();
    }
}

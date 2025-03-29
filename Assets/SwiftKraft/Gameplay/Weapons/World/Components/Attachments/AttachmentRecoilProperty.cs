using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentRecoilProperty : AttachmentStatisticPropertyBase<WeaponRecoil>
    {
        public override ModifiableStatistic.Modifier CreateOverrider() => Component.RecoilMultiplier.AddModifier();
    }
}

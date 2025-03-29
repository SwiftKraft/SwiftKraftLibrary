using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentMaxAmmoProperty : AttachmentStatisticPropertyBase<WeaponAmmo>
    {
        public override ModifiableStatistic.Modifier CreateOverrider() => Component.MaxAmmo.AddModifier();
    }
}

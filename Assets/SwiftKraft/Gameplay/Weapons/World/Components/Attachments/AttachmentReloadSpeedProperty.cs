using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentReloadSpeedProperty : AttachmentStatisticPropertyBase<WeaponAmmo>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() => new AttachmentReloadSpeedProperty()
        {
            value = value,
            type = type,
        };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.ReloadSpeedMultiplier.AddModifier();
    }
}

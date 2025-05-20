using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentDamageProperty : AttachmentStatisticPropertyBase<WeaponBase>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() => new AttachmentDamageProperty()
        {
            type = type,
            value = value,
        };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.Damage.AddModifier();
    }
}

using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentRecoilProperty : AttachmentStatisticPropertyBase<WeaponRecoil>
    {
        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() =>
            new AttachmentAimSpeedProperty()
            {
                value = value,
                type = type,
            };

        public override ModifiableStatistic.Modifier CreateOverrider() => Component.RecoilMultiplier.AddModifier();
    }
}

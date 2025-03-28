using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentRecoilProperty : AttachmentComponentPropertyBase<WeaponRecoil>
    {
        public float recoilValue;
        public ModifiableStatistic.ModifierType recoilType;

        public float decayValue;
        public ModifiableStatistic.ModifierType decayType;

        ModifiableStatistic.Modifier recoilModifier;
        ModifiableStatistic.Modifier decayModifier;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            recoilModifier = Component.RecoilMultiplier.AddModifier();
            decayModifier = Component.DecayMultiplier.AddModifier();
        }

        public override void Update()
        {
            if (recoilModifier != null)
            {
                recoilModifier.Value = recoilValue;
                recoilModifier.Type = recoilType;
            }

            if (decayModifier != null)
            {
                decayModifier.Value = decayValue;
                decayModifier.Type = decayType;
            }
        }

        public override void Uninstall()
        {
            base.Uninstall();
            recoilModifier.Value = 1f;
            recoilModifier.Type = ModifiableStatistic.ModifierType.Multiplication;
            decayModifier.Value = 1f;
            decayModifier.Type = ModifiableStatistic.ModifierType.Multiplication;
        }

        public override void Destroy()
        {
            base.Destroy();
            recoilModifier.Dispose();
            decayModifier.Dispose();
        }
    }
}

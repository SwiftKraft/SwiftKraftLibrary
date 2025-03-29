using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class AttachmentAimFOVProperty : AttachmentComponentPropertyBase<WeaponAdsZoomer>
    {
        public float viewFOV;
        public ModifiableStatistic.ModifierType viewType;

        public float modelFOV;
        public ModifiableStatistic.ModifierType modelType;

        ModifiableStatistic.Modifier viewFOVModifier;
        ModifiableStatistic.Modifier modelFOVModifier;

        public override void Init(WeaponAttachmentSlot.Attachment parent)
        {
            base.Init(parent);
            viewFOVModifier = Component.AimCameraFOV.AddModifier();
            modelFOVModifier = Component.AimViewModelFOV.AddModifier();
        }

        public override void Update()
        {
            if (viewFOVModifier != null)
            {
                viewFOVModifier.Value = viewFOV;
                viewFOVModifier.Type = viewType;
            }

            if (modelFOVModifier != null)
            {
                modelFOVModifier.Value = modelFOV;
                modelFOVModifier.Type = modelType;
            }
        }

        public override void Uninstall()
        {
            base.Uninstall();
            viewFOVModifier.Value = 1f;
            viewFOVModifier.Type = ModifiableStatistic.ModifierType.Multiplication;
            modelFOVModifier.Value = 1f;
            modelFOVModifier.Type = ModifiableStatistic.ModifierType.Multiplication;
        }

        public override void Destroy()
        {
            base.Destroy();
            viewFOVModifier.Dispose();
            modelFOVModifier.Dispose();
        }
    }
}

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

        public override void Init(WeaponAttachmentSlotScriptable.Attachment parent)
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
            viewFOVModifier.Value = 0f;
            viewFOVModifier.Type = ModifiableStatistic.ModifierType.Addition;
            modelFOVModifier.Value = 0f;
            modelFOVModifier.Type = ModifiableStatistic.ModifierType.Addition;
        }

        public override void Destroy()
        {
            base.Destroy();
            viewFOVModifier.Dispose();
            modelFOVModifier.Dispose();
        }

        public override WeaponAttachmentSlotScriptable.AttachmentProperty Clone() => 
            new AttachmentAimFOVProperty()
            {
                viewFOV = viewFOV,
                viewType = viewType,
                modelFOV = modelFOV,
                modelType = modelType
            };
    }
}

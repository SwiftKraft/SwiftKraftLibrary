using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class AttachmentStatisticPropertyBase<T> : AttachmentOverridePropertyBase<T, ModifiableStatistic.Modifier> where T : Component
    {
        public float value;
        public ModifiableStatistic.ModifierType type;

        public override void ApplyOverrides()
        {
            overrider.Value = value;
            overrider.Type = type;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            overrider.Value = 0f;
            overrider.Type = ModifiableStatistic.ModifierType.Addition;
        }
    }
}

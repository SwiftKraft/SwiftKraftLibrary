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
            overrider.Type = type;
            overrider.Value = value;
        }

        public override void Uninstall()
        {
            base.Uninstall();
            overrider.Type = ModifiableStatistic.ModifierType.Addition;
            overrider.Value = 0f;
        }
    }
}

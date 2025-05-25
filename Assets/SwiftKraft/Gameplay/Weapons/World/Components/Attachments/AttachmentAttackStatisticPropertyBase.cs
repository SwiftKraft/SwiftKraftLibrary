using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class AttachmentAttackStatisticPropertyBase<T> : AttachmentStatisticPropertyBase<WeaponBase> where T : WeaponAttackBase
    {
        public int AttackModeIndex = 0;

        public T Attacker
        {
            get
            {
                if (cache != null)
                    return cache;

                WeaponAttackBase attacker = Component.AttackModes[Component.AttackModes.WrapIndex(AttackModeIndex)];
                if (attacker is T t)
                    cache = t;
                return cache;
            }
        }
        T cache;
    }
}

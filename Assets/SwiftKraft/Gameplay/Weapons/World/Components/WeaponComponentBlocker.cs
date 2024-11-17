using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponComponentBlocker : WeaponComponent
    {
        protected BooleanLock.Lock AttackDisabler { get; private set; }

        protected virtual void Awake() => AttackDisabler = Parent.CanAttack.AddLock();

        protected virtual void OnDestroy() => Parent.CanAttack.RemoveLock(AttackDisabler);
    }
}

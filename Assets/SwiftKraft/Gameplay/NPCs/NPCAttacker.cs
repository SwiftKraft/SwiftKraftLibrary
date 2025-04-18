using SwiftKraft.Gameplay.Weapons;

namespace SwiftKraft.Gameplay.NPCs
{
    public abstract class NPCAttacker : NPCModuleBase
    {
        public WeaponBase Weapon;

        protected NPCScannerBase.Package ScannerData;

        public bool HasTarget => (ScannerData != null || Parent.Values.TryGet(NPCScannerBase.DataID, out ScannerData)) && ScannerData.Targets.Count >= 0;

        protected override void Awake()
        {
            base.Awake();
            if (Weapon == null)
                Weapon = GetComponentInChildren<WeaponBase>();
        }

        protected virtual void Start() => ScannerData = Parent.Values.Get<NPCScannerBase.Package>(NPCScannerBase.DataID);

        public virtual void Attack() => Weapon.Attack();
    }
}

using SwiftKraft.Gameplay.Weapons;

namespace SwiftKraft.Gameplay.NPCs
{
    public abstract class NPCAttacker : NPCModuleBase
    {
        protected NPCScannerBase.Package ScannerData;

        public virtual bool HasTarget => (ScannerData != null || Parent.Values.TryGet(NPCScannerBase.DataID, out ScannerData)) && ScannerData.Targets.Count >= 0;

        public abstract bool CanAttack { get; }

        protected virtual void Start() => ScannerData = Parent.Values.Get<NPCScannerBase.Package>(NPCScannerBase.DataID);

        protected virtual void FixedUpdate()
        {
            if (HasTarget && CanAttack)
                Attack();
        }

        public abstract void Attack();
    }
}

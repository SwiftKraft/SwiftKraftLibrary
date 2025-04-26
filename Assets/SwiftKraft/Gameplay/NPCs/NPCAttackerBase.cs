using SwiftKraft.Gameplay.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public abstract class NPCAttackerBase : NPCModuleBase
    {
        protected NPCScannerBase.Package ScannerData;

        public List<KeyValuePair<ITargetable, Transform>> Targets => ScannerData.Targets;

        public virtual bool HasTarget => (ScannerData != null || Parent.Values.TryGet(NPCScannerBase.DataID, out ScannerData)) && Targets.Count > 0;

        public abstract bool CanAttack { get; }

        public bool AllowAttack { get; set; }

        protected virtual void Start() => ScannerData = Parent.Values.Get<NPCScannerBase.Package>(NPCScannerBase.DataID);

        protected virtual void FixedUpdate()
        {
            if (AllowAttack && CanAttack)
                Attack();
        }

        public abstract void Attack();
    }

    public abstract class NPCSingleTargetAttacker : NPCAttackerBase
    {
        public KeyValuePair<ITargetable, Transform> CurrentTarget => HasTarget ? Targets[0] : default;

        public override bool CanAttack => HasTarget && CurrentTarget.Key != null && CurrentTarget.Value != null;
    }
}

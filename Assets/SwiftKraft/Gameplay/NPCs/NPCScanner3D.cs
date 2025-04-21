using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCScanner3D : NPCScannerBase
    {
        public Collider Collider { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Collider = GetComponent<Collider>();
        }

        public override bool CheckLOS(Vector3 targetPos, GameObject targetCollider = null)
        {
            Collider targetCol = null;
            bool colliderState = Collider.enabled;
            bool targetHasCollider = targetCollider != null && targetCollider.TryGetComponent(out targetCol);
            bool targetColliderState = true;

            if (targetHasCollider)
            {
                targetColliderState = targetCol.enabled;
                targetCol.enabled = false;
            }

            Collider.enabled = false;

            bool los = !Physics.Linecast(SightPoint.position, targetPos, LOSLayers, QueryTriggerInteraction.Ignore);
            
            Collider.enabled = colliderState;

            if (targetHasCollider)
                targetCol.enabled = targetColliderState;

            return los;
        }

        public override Dictionary<ITargetable, Transform> AcquireTargets()
        {
            Dictionary<ITargetable, Transform> targetables = new();
            Collider[] cols = Physics.OverlapSphere(SightPoint.position, ScanRange);
            foreach (Collider col in cols)
            {
                if ((col.TryGetComponentInParent(out ITargetable targetable) || col.TryGetComponent(out targetable)) && CheckTargetLOS(targetable, out Transform valid))
                    targetables.Add(targetable, valid);
            }
            return targetables;
        }
    }
}

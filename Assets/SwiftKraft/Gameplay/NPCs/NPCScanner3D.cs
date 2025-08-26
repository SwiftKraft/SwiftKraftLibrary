using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCScanner3D : NPCScannerBase
    {
        public Collider Collider { get; private set; }

        public ICollider3D Colliders { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Collider = GetComponent<Collider>();
            Colliders = GetComponentInChildren<ICollider3D>();
        }

        public override bool CheckLOS(Vector3 targetPos, GameObject targetObject = null) => !Physics.Linecast(SightPoint.position, targetPos, LOSLayers, QueryTriggerInteraction.Ignore);

        public override Dictionary<ITargetable, Transform> AcquireTargets()
        {
            Dictionary<ITargetable, Transform> targetables = new();
            Collider[] cols = Physics.OverlapSphere(SightPoint.position, ScanRange);
            foreach (Collider col in cols)
            {
                if ((col.TryGetComponentInParent(out ITargetable targetable) || col.TryGetComponent(out targetable)) && !targetables.ContainsKey(targetable) && CheckTargetLOS(targetable, out Transform valid))
                    targetables.Add(targetable, valid);
            }
            return targetables;
        }

        protected override void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(SightPoint.position, ScanRange);
        }
    }
}

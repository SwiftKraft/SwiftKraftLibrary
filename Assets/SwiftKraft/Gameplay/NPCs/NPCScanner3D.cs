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

        public override bool CheckLOS(Vector3 targetPos, GameObject targetObject = null)
        {
            ICollider3D targetColliders = null;
            Collider targetCol = null;
            bool colliderState = Collider.enabled;
            bool[] colliderStates = Colliders != null ? new bool[Colliders.Colliders.Length] : new bool[0];
            bool targetHasCollider = targetObject != null && (targetObject.TryGetComponent(out targetCol) || targetObject.TryGetComponentInParent(out targetCol));
            bool targetHasColliders = targetObject != null && (targetObject.TryGetComponent(out targetColliders) || targetObject.TryGetComponentInParent(out targetColliders));
            bool targetColliderState = true;
            bool[] targetColliderStates = targetColliders != null ? new bool[targetColliders.Colliders.Length] : new bool[0];

            if (Colliders != null)
                for (int i = 0; i < Colliders.Colliders.Length; i++)
                    colliderStates[i] = Colliders.Colliders[i].enabled;

            if (targetHasColliders)
                for (int i = 0; i < targetColliders.Colliders.Length; i++)
                    targetColliderStates[i] = targetColliders.Colliders[i].enabled;

            if (targetHasCollider)
            {
                targetColliderState = targetCol.enabled;
                targetCol.enabled = false;
            }

            Collider.enabled = false;
            Colliders?.SetActive(false);
            targetColliders?.SetActive(false);

            bool los = !Physics.Linecast(SightPoint.position, targetPos, LOSLayers, QueryTriggerInteraction.Ignore);

            Collider.enabled = colliderState;
            Colliders?.SetActive(colliderStates);
            targetColliders?.SetActive(targetColliderStates);

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
                if ((col.TryGetComponentInParent(out ITargetable targetable) || col.TryGetComponent(out targetable)) && !targetables.ContainsKey(targetable) && CheckTargetLOS(targetable, out Transform valid))
                    targetables.Add(targetable, valid);
            }
            return targetables;
        }
    }
}

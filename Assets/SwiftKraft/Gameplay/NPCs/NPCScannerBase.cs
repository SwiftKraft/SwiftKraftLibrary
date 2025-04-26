using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    [DisallowMultipleComponent]
    public abstract class NPCScannerBase : NPCModuleBase
    {
        public const string DataID = "Essentials.Scanner";
        public override string ID => DataID;

        public class Package
        {
            public NPCScannerBase Parent { get; private set; }
            public readonly List<KeyValuePair<ITargetable, Transform>> Targets = new();

            public void Init(NPCScannerBase scn) => Parent = scn;

            public void Sort() => Targets.Sort((a, b) => (int)(Score(b.Key) - Score(a.Key)).GetSign());

            private float Score(ITargetable target) =>
                CalculateTargetScore
                (
                    Vector3.Distance(Parent.transform.position, target.GameObject.transform.position),
                    Parent.ScanRange, target.Priority, Parent.PriorityWeight
                );
        }

        public LayerMask LOSLayers;
        public float ScanRange = 50f;
        public float PriorityWeight = 0.7f;

        public Timer ScanTimer;

        public Transform SightPoint;

        public readonly Package Data = new();

        public virtual bool HasTarget => Data.Targets.Count > 0;

        public List<KeyValuePair<ITargetable, Transform>> Targets => Data.Targets;

        protected override void Awake()
        {
            base.Awake();
            Data.Init(this);
            Parent.Values.Add(ID, Data);
        }

        protected virtual void FixedUpdate()
        {
            ScanTimer.Tick(Time.fixedDeltaTime);
            if (ScanTimer.Ended)
            {
                Scan();
                ScanTimer.Reset();
            }
        }

        public void Scan()
        {
            Data.Targets.Clear();
            Dictionary<ITargetable, Transform> targets = AcquireTargets();
            foreach (KeyValuePair<ITargetable, Transform> target in targets)
            {
                if (ValidTarget(target.Key))
                    Data.Targets.Add(target);
            }
            Data.Sort();
        }

        public virtual bool ValidTarget(ITargetable target) => target.CanTarget && target.Faction != Parent.Faction;

        public abstract Dictionary<ITargetable, Transform> AcquireTargets();

        public abstract bool CheckLOS(Vector3 targetPos, GameObject target = null);

        public virtual bool CheckTargetLOS(ITargetable target, out Transform valid)
        {
            foreach (Transform tr in target.SightPoints)
                if (CheckLOS(tr.position, tr.gameObject))
                {
                    valid = tr;
                    return true;
                }

            valid = null;
            return false;
        }

        protected virtual void OnDrawGizmos()
        {
            if (Data.Targets.Count <= 0)
                return;

            foreach (KeyValuePair<ITargetable, Transform> k in Data.Targets)
            {
                Gizmos.color = ValidTarget(k.Key) ? Color.red : Color.green;
                Gizmos.DrawLine(SightPoint.position, k.Value.position);
            }
        }

        public static float CalculateTargetScore(float distance, float maxDistance, float priority, float weight)
        {
            distance = Mathf.Clamp(distance, 0f, maxDistance);

            float priorityComponent = (1f - weight) * priority;
            float distanceComponent = weight * (1f - (distance / maxDistance));

            float score = priorityComponent + distanceComponent;
            return score;
        }
    }
}

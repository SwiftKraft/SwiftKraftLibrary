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
            public readonly List<ITargetable> Targets = new();

            public void Init(NPCScannerBase scn) => Parent = scn;

            public void Sort() => Targets.Sort((a, b) => (int)(Score(b) - Score(a)).GetSign());

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

        protected override void Awake()
        {
            base.Awake();
            Data.Init(this);
            Parent.Values.Add(ID, Data);
        }

        protected virtual void FixedUpdate()
        {

        }

        public void Scan()
        {
            Data.Targets.Clear();
            ITargetable[] targets = AcquireTargets();
            foreach (ITargetable target in targets)
            {
                if (ValidTarget(target))
                    Data.Targets.Add(target);
            }
            Data.Sort();
        }

        public virtual bool ValidTarget(ITargetable target) => target.Faction != Parent.Faction && CheckLOS(target.GameObject.transform.position, target.GameObject);

        public abstract ITargetable[] AcquireTargets();

        public abstract bool CheckLOS(Vector3 targetPos, GameObject target = null);

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

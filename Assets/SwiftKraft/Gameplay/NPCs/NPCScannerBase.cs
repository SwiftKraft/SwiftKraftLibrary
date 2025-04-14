using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    [DisallowMultipleComponent]
    public abstract class NPCScannerBase : NPCModuleBase
    {
        public override string ID => "Essentials.Scanner";

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

        public readonly Package Data = new();

        protected override void Awake()
        {
            base.Awake();
            Data.Init(this);
            Parent.Values.Add(ID, Data);
        }

        public virtual bool ValidTarget(ITargetable target) => CheckLOS(target.GameObject.transform.position);

        public abstract bool CheckLOS(Vector3 targetPos);

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

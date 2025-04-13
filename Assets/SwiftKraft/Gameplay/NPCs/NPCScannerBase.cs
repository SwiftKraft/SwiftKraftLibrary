using SwiftKraft.Gameplay.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public abstract class NPCScannerBase : NPCModuleBase
    {
        public override string ID => "Essentials.Scanner";

        public class Package
        {
            public readonly List<IFaction> Targets = new();

            public void Sort()
            {
                Targets.Sort((a, b) => a.Faction.CompareTo(b.Faction));
            }
        }

        public LayerMask LOSLayers;
        public float LOSRange = 50f;

        private Package Data;

        protected override void Awake()
        {
            base.Awake();
            Parent.Values.Add("Scanner", this);
        }

        protected virtual void FixedUpdate()
        {

        }

        public abstract bool CheckLOS(Vector3 targetPos);
    }
}

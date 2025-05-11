using SwiftKraft.Gameplay.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Gameplay.Projectiles
{
    public abstract class ProjectileHitscan : ProjectileBase
    {
        public float Range = 100f;
        public LayerMask Layers;
        public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;

        public int Pierce = 1;

        public RaycastHit[] Hits { get; private set; }

        public event Action OnCast;

        protected virtual void Start()
        {
            Hits = Cast();
            Array.Sort(Hits, (p1, p2) => p1.distance > p2.distance ? 1 : -1);
            List<RaycastHit> hitList = Hits.ToList();
            hitList.RemoveAll((h) => h.transform.TryGetComponent(out IPawn pawn) && pawn == Owner);
            Hits = hitList.ToArray();
            Hit(Hits);
            OnCast?.Invoke();

#if UNITY_EDITOR
            if (Hits.Length > 0)
                for (int i = 0; i < Hits.Length; i++)
                    if (i > 0)
                        Debug.DrawLine(Hits[i].point, Hits[i - 1].point, Color.HSVToRGB(i * (1f / Pierce) % 1f, 1f, 1f), 5f);
                    else
                        Debug.DrawLine(transform.position, Hits[i].point, Color.HSVToRGB(i * (1f / Pierce) % 1f, 1f, 1f), 5f);
            else
                Debug.DrawLine(transform.position, transform.position + transform.forward * Range, Color.HSVToRGB(0f, 1f, 1f), 5f);
#endif  
        }

        public abstract RaycastHit[] Cast();

        public abstract void Hit(RaycastHit[] hits);
    }
}

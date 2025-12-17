using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Debugging
{
    public class TransformTracker : MonoBehaviour
    {
        public bool OnlyShowSelected = false;
        [SerializeReference, Subclass]
        public GizmosDrawer[] CurrentDrawers;

        public TransformDataScale PreviousWorld { get; private set; }
        public TransformDataScale PreviousLocal { get; private set; }

        private void Awake()
        {
            PreviousWorld = new(transform);
            PreviousLocal = new(transform, true);
        }

        public void Track()
        {
            if (!transform.hasChanged)
                return;

            transform.hasChanged = false;

            foreach (GizmosDrawer drawer in CurrentDrawers)
                drawer.Draw(this);

            PreviousWorld = new(transform);
            PreviousLocal = new(transform, true);
        }

        private void OnDrawGizmos()
        {
            if (OnlyShowSelected)
                return;

            Track();
        }

        private void OnDrawGizmosSelected()
        {
            if (!OnlyShowSelected)
                return;

            Track();
        }

        [Serializable]
        public abstract class GizmosDrawer
        {
            public abstract void Draw(TransformTracker tracker);
        }
    }

    public class TransformTrackerLine : TransformTracker.GizmosDrawer
    {
        public Color Color = Color.white;
        public float Duration = 1f;

        public override void Draw(TransformTracker tracker) => Debug.DrawLine(tracker.transform.position, tracker.PreviousWorld.Position, Color, Duration);
    }

    public class TransformTrackerSphere : TransformTracker.GizmosDrawer
    {
        public Color Color = Color.white;
        public float Radius = 0.25f;

        public override void Draw(TransformTracker tracker)
        {
            Gizmos.matrix = tracker.transform.localToWorldMatrix;
            Gizmos.color = Color;
            Gizmos.DrawWireSphere(default, Radius);
        }
    }
}

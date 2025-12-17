using SwiftKraft.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Debugging
{
    public class TransformTracker : MonoBehaviour
    {
        public bool OnlyShowSelected = false;
        [Subclass]
        public GizmosDrawer[] CurrentDrawers;

        public TransformDataScale Previous { get; private set; }

        private void Awake() => Previous = new(transform);

        public void Track()
        {
            if (!transform.hasChanged)
                return;

            transform.hasChanged = false;

            foreach (GizmosDrawer drawer in CurrentDrawers)
                drawer.Draw(this);

            Previous = new(transform);
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

        public override void Draw(TransformTracker tracker)
        {
            Debug.DrawLine(tracker.transform.position, tracker.Previous.Position, Color, Duration);
        }
    }
}

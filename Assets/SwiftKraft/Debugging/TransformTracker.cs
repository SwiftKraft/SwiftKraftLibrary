using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Debugging
{
    public class TransformTracker : MonoBehaviour
    {
        public bool OnlyShowSelected = false;
        [SerializeReference, Subclass]
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

        public override void Draw(TransformTracker tracker) => Debug.DrawLine(tracker.transform.position, tracker.Previous.Position, Color, Duration);
    }

    public class TransformTrackerSphere : TransformTracker.GizmosDrawer
    {
        public Color Color = Color.white;
        public float Radius = 0.25f;
        public bool Trail = false;
        public int Length = 10;

        TransformDataScale[] array;
        int availableLength = 0;

        public override void Draw(TransformTracker tracker)
        {
            if (array == null || array.Length != Length)
            {
                array = new TransformDataScale[Length];
                availableLength = 0;
            }

            if (availableLength < Length)
            array[++availableLength - 1] = tracker.Previous;
            else
                array.s
        }
    }
}

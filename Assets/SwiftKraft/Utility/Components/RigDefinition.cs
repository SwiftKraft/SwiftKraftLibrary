using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class RigDefinition : MonoBehaviour
    {
        public struct ReplicationData
        {
            public TransformData Root;
            public TransformData[] Transforms;

            public ReplicationData(RigDefinition def)
            {
                Root = new TransformData(def.Root, false);
                Transforms = new TransformData[def.Transforms.Count];
                for (int i = 0; i < def.Transforms.Count; i++)
                    Transforms[i] = new TransformData(def.Transforms[i], true);
            }
        }

        public Transform Root;

        public List<Transform> Transforms = new();

        public Animator Animator { get; private set; }

        ReplicationData replicationQueue;
        bool queued;

        private void Awake() => Animator = GetComponentInChildren<Animator>();

        private void LateUpdate()
        {
            if (queued)
            {
                ReplicateInternal(replicationQueue);
                queued = false;
            }
        }

        public void Replicate(RigDefinition source)
        {
            if (source == null) return;

            if (source.Animator != null && source.Animator.enabled)
            {
                replicationQueue = new ReplicationData(source);
                queued = true;
                return;
            }

            ReplicateInternal(source);
        }

        private void ReplicateInternal(RigDefinition source) => ReplicateInternal(new ReplicationData(source));

        private void ReplicateInternal(ReplicationData source)
        {
            if (Root == null)
                return;

            Root.SetPositionAndRotation(source.Root.Position, source.Root.Rotation);
            int min = Mathf.Min(source.Transforms.Length, Transforms.Count);
            for (int i = 0; i < min; i++)
                Transforms[i].SetLocalPositionAndRotation(source.Transforms[i].Position, source.Transforms[i].Rotation);
        }

        public void Rebuild()
        {
            if (Root == null)
            {
                Debug.LogWarning("Root is null! ");
                return;
            }

            Transforms.Clear();
            RecursiveBuild(Root);

            Debug.Log($"Rebuilt rig definition from root node: \"{Root.name}\", collecting {Transforms.Count} transforms.");
        }

        private void RecursiveBuild(Transform cur)
        {
            if (cur != Root)
                Transforms.Add(cur);

            if (cur.childCount <= 0)
                return;

            foreach (Transform t in cur)
                RecursiveBuild(t);
        }
    }
}

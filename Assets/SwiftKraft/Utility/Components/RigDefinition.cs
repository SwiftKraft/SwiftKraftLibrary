using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class RigDefinition : MonoBehaviour
    {
        public Transform Root;

        public List<Transform> Transforms = new();

        public Animator Animator { get; private set; }

        private void Awake() => Animator = GetComponentInChildren<Animator>();

        public void Replicate(RigDefinition source, bool moveUnregistered = true)
        {
            if (source == null) return;

            if (source.Animator != null)
                source.Animator.Update(0f);

            int min = Mathf.Min(source.Transforms.Count, Transforms.Count);
            for (int i = 0; i < min; i++)
                Transforms[i].SetLocalPositionAndRotation(source.Transforms[i].localPosition, source.Transforms[i].localRotation);

            if (!moveUnregistered)
                return;
            
            for (int i = 0; i < min; i++)
                foreach (Transform tr in source.Transforms[i])
                    if (!source.Transforms.Contains(tr))
                        tr.SetParent(Transforms[i], false);
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
            Transforms.Add(cur);

            if (cur.childCount <= 0)
                return;

            foreach (Transform t in cur)
                RecursiveBuild(t);
        }
    }
}

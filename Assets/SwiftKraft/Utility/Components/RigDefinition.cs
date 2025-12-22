using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class RigDefinition : MonoBehaviour
    {
        public struct TransformDataNode
        {
            public TransformData Transform;
            public TransformDataNode[] Children;

            public TransformDataNode(TransformNode node)
            {
                Transform = new(node.Transform);
                Children = new TransformDataNode[node.Children.Length];

                if (Children.Length <= 0)
                    return;

                for (int i = 0; i < Children.Length; i++)
                    Children[i] = new(node.Children[i]);
            }
        }

        public struct TransformNode
        {
            public Transform Transform;
            public TransformNode[] Children;

            public TransformNode(Transform tr)
            {
                Transform = tr;
                Children = new TransformNode[Transform.childCount];

                if (Children.Length <= 0)
                    return;

                for (int i = 0; i < Children.Length; i++)
                    Children[i] = new(Transform.GetChild(i));
            }
        }

        public Transform Root;
        public TransformNode RootNode { get; private set; }

        TransformDataNode queuedReplication;
        bool queued;

        private void LateUpdate()
        {
            if (queued)
            {
                queued = false;
                ReplicateData(queuedReplication);
            }
        }

        public void Replicate(RigDefinition source, bool moveUnregistered = false)
        {
            if (source == null) return;

            queued = true;
            queuedReplication = new TransformDataNode(source.RootNode);
        }

        private void ReplicateData(TransformDataNode source)
        {
            if (Root == null)
                return;

            ReplicateRecursve(RootNode, source);
        }

        private void ReplicateRecursve(TransformNode cur, TransformDataNode data)
        {
            cur.Transform.SetPositionAndRotation(data.Transform.Position, data.Transform.Rotation);

            if (cur.Children.Length <= 0)
                return;

            for (int i = 0; i < cur.Children.Length; i++)
                ReplicateRecursve(cur.Children[i], data.Children[i]);
        }

        public void Rebuild()
        {
            if (Root == null)
            {
                Debug.LogWarning("Root is null! ");
                RootNode = default;
                return;
            }

            RootNode = new(Root);

            Debug.Log($"Rebuilt rig definition from root node: \"{Root.name}\"");
        }
    }
}

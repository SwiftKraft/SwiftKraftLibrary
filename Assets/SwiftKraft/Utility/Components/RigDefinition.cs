using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class RigDefinition : MonoBehaviour
    {
        public Transform Root;

        [SerializeField]
        private RigNode[] nodes;

        private Dictionary<string, int> pathToIndex;

        [System.Serializable]
        private struct RigNode
        {
            public string Path;
            public int ParentIndex;
            public Transform Transform;
        }

        // ---------- PUBLIC API ----------

        public void Replicate(RigDefinition source, bool moveUnregistered = true)
        {
            if (source == null || source.nodes == null)
                return;

            EnsureMap();
            source.EnsureMap();

            foreach (var src in source.nodes)
            {
                if (!pathToIndex.TryGetValue(src.Path, out int dstIndex))
                    continue;

                var dst = nodes[dstIndex];

                dst.Transform.SetLocalPositionAndRotation(src.Transform.localPosition, src.Transform.localRotation);

                if (!moveUnregistered)
                    continue;

                for (int i = 0; i < src.Transform.childCount; i++)
                {
                    Transform child = src.Transform.GetChild(i);

                    if (source.pathToIndex.ContainsKey(child.name))
                        continue;

                    child.SetParent(dst.Transform, worldPositionStays: true);
                }
            }
        }

        public void Rebuild()
        {
            if (Root == null)
            {
                nodes = null;
                pathToIndex = null;
                Debug.LogWarning("RigDefinition: Root is null, cannot rebuild rig definition.");
                return;
            }

            var list = new List<RigNode>();
            BuildFlat(Root, -1, "", list);
            nodes = list.ToArray();
            pathToIndex = null;
            Debug.Log($"RigDefinition: Rebuilt rig definition with {nodes.Length} nodes.");
        }

        // ---------- INTERNAL ----------

        private void BuildFlat(
            Transform t,
            int parentIndex,
            string parentPath,
            List<RigNode> list)
        {
            string path = string.IsNullOrEmpty(parentPath)
                ? t.name
                : $"{parentPath}/{t.name}";

            int index = list.Count;

            list.Add(new RigNode
            {
                Path = path,
                ParentIndex = parentIndex,
                Transform = t
            });

            for (int i = 0; i < t.childCount; i++)
                BuildFlat(t.GetChild(i), index, path, list);
        }

        private void EnsureMap()
        {
            if (pathToIndex != null)
                return;

            pathToIndex = new Dictionary<string, int>(nodes.Length);
            for (int i = 0; i < nodes.Length; i++)
                pathToIndex[nodes[i].Path] = i;
        }
    }
}

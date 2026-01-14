using UnityEngine;


namespace SwiftKraft.Utils
{
    public class HierarchyCopier : MonoBehaviour
    {
        [SerializeField] private Transform sourceRoot;
        [SerializeField] private Transform targetRoot;

        [ContextMenu("Copy Local Transforms")]
        public void Copy() => Copy(sourceRoot, targetRoot);
        /// <summary>
        /// Source to target copying, target gets changed.
        /// </summary>
        /// <param name="sourceRoot"></param>
        /// <param name="targetRoot"></param>
        public static void Copy(Transform sourceRoot, Transform targetRoot, bool useWorldPos = false)
        {
            if (sourceRoot == null || targetRoot == null)
            {
                Debug.LogError("Source or Target root is null.");
                return;
            }

            CopyRecursive(sourceRoot, targetRoot, useWorldPos);
        }

        private static void CopyRecursive(Transform source, Transform target, bool useWorldPos = false)
        {
            if (useWorldPos)
                target.SetPositionAndRotation(source.position, source.rotation);
            else
                target.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
            target.localScale = source.localScale;

            int count = Mathf.Min(source.childCount, target.childCount);
            for (int i = 0; i < count; i++)
                CopyRecursive(source.GetChild(i), target.GetChild(i));
        }
    }
}

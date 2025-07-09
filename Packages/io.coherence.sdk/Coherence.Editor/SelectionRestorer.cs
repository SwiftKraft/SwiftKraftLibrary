namespace Coherence.Editor
{
    using UnityEditor;

    // This class exists to circumvent an issue with Unity where the selection gets lost.
    // See https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-61690

    [InitializeOnLoad]
    internal static class SelectionRestorer
    {
        private static int[] cachedSelection;
        private const string key = "Coherence.SelectionRestorer.Cache";

        static SelectionRestorer()
        {
            cachedSelection = SessionState.GetIntArray(key, null);
            Selection.selectionChanged += OnSelectionChanged;
        }

        /// <summary>
        /// Restore to the last known selection on the next Editor frame
        /// (via <see cref="UnityEditor.EditorApplication.delayCall"/>)
        /// </summary>
        public static void RequestRestore()
        {
            EditorApplication.delayCall -= RestoreSelection;
            EditorApplication.delayCall += RestoreSelection;
        }

        private static void OnSelectionChanged()
        {
            var selection = Selection.instanceIDs;
            if (Selection.instanceIDs.Length <= 0)
            {
                return;
            }

            cachedSelection = selection;
            SessionState.SetIntArray(key, cachedSelection);
        }

        private static void RestoreSelection()
        {
            if (cachedSelection != null)
            {
                Selection.instanceIDs = cachedSelection;
            }
        }
    }
}

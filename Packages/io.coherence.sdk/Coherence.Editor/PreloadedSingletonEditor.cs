namespace Coherence.Editor
{
    using UnityEditor;

    [CustomEditor(typeof(PreloadedSingleton))]
    internal class PreloadedSingletonEditor : BaseEditor
    {
        private PreloadedSingleton singleton;

        protected override void OnEnable()
        {
            singleton = target as PreloadedSingleton;
        }

        protected override void OnAfterHeader()
        {
            if (!singleton.IsActiveInstance)
            {
                EditorGUILayout.HelpBox("This asset is not the active instance.",
                    MessageType.Warning);
            }
        }
    }
}

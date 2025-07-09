namespace Coherence.Editor
{
    using Build;
    using UnityEditor;

    [CustomEditor(typeof(SimulatorBuildOptions))]
    internal class SimulatorBuildOptionsEditor : BaseEditor
    {
        protected override void OnGUI()
        {
            DrawPropertiesExcluding(serializedObject, "m_Script");
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.UI
{
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [UsedImplicitly]
    [CustomEditor(typeof(SampleDialogAsset))]
    public class SampleDialogAssetEditor : Editor
    {
        private PropertyField MakeDefaultField(string label, string propertyName, string tooltip = "")
        {
            var property = serializedObject.FindProperty(propertyName);
            var field = new PropertyField(property, label)
            {
                tooltip = string.IsNullOrEmpty(tooltip) ? label : tooltip,
            };
            return field;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            root.Add(MakeDefaultField("Enabled", nameof(SampleDialogAsset.Enabled)));
            root.Add(MakeDefaultField("Display Name", nameof(SampleDialogAsset.SampleDisplayName)));
            root.Add(MakeDefaultField("Prefab Filename", nameof(SampleDialogAsset.PrefabFileName)));
            root.Add(MakeDefaultField("Dependency", nameof(SampleDialogAsset.Dependency)));
            root.Add(MakeDefaultField("Category", nameof(SampleDialogAsset.SampleCategory)));
            root.Add(MakeDefaultField("Category Detail", nameof(SampleDialogAsset.CategoryDetail)));
            root.Add(MakeDefaultField("External Url", nameof(SampleDialogAsset.ExternalUrl)));
            root.Add(MakeDefaultField("Priority", nameof(SampleDialogAsset.Priority)));

            var descriptionField = new TextField("Description")
            {
                multiline = true,
                style = { whiteSpace = WhiteSpace.Normal },
            };
            descriptionField.bindingPath = nameof(SampleDialogAsset.Description);
            root.Add(descriptionField);

            root.Add(MakeDefaultField("Preview Image", nameof(SampleDialogAsset.Preview)));
            root.Add(MakeDefaultField("Grid Thumbnail", nameof(SampleDialogAsset.Thumbnail)));
            return root;
        }
    }
}

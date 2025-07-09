// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using UnityEngine.UIElements;

namespace Coherence.UI
{
    using UnityEditor;
    using UnityEditor.UIElements;

    internal class SampleDialogPreview : VisualElement
    {
        private const string DefaultTitle = "No dialog selected.";
        private const string DefaultDescription = "";

        private VisualElement thumbnailContainer;
        private Label titleLabel;
        private Label categoryLabel;
        private Label descriptionLabel;

        public SampleDialogAsset SampleDialog
        {
            get => sampleDialog;
            set
            {
                sampleDialog = value;
                UpdateTemplateDescriptionUI();
            }
        }

        private SampleDialogAsset sampleDialog;

        public SampleDialogPreview()
        {
            AddToClassList(Styles.classDescriptionContainer);
            EnableInClassList(Styles.classDescriptionContainerDark, EditorGUIUtility.isProSkin);

            thumbnailContainer = new VisualElement();
            thumbnailContainer.AddToClassList(Styles.classPreviewArea);
            this.Add(thumbnailContainer);

            var container = new VisualElement();
            container.AddToClassList(Styles.previewTitleCategoryContainer);

            titleLabel = new Label(DefaultTitle);
            titleLabel.bindingPath = nameof(SampleDialogAsset.SampleDisplayName);
            titleLabel.AddToClassList(Styles.classPreviewTitleLabel);
            container.Add(titleLabel);

            categoryLabel = new Label("Package Sample");
            categoryLabel.bindingPath = nameof(SampleDialogAsset.CategoryDetail);
            categoryLabel.AddToClassList(Styles.gridViewCategoryBorder);
            container.Add(categoryLabel);
            this.Add(container);

            var scrollView = new ScrollView();
            {
                descriptionLabel = new Label(DefaultDescription);
                descriptionLabel.bindingPath = nameof(SampleDialogAsset.Description);
                descriptionLabel.AddToClassList(Styles.classPreviewTextLabel);
                scrollView.Add(descriptionLabel);
            }
            this.Add(scrollView);
        }

        private void UpdateTemplateDescriptionUI()
        {
            if (sampleDialog == default)
            {
                this.Unbind();
                thumbnailContainer.style.backgroundImage = null;
                titleLabel.text = DefaultTitle;
                descriptionLabel.text = DefaultDescription;
            }
            else
            {
                this.Bind(new SerializedObject(sampleDialog));
                thumbnailContainer.style.backgroundImage = new StyleBackground(sampleDialog.Preview);
            }
        }
    }
}

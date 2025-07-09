namespace Coherence.MatchmakingDialogSample.UI
{
    using UnityEngine.UIElements;

#if UNITY_2023_1_OR_NEWER
    [UxmlElement]
#endif
    public partial class DisconnectButton : VisualElement
    {
        private const string BtnLabel = "Disconnect";

        private const string ClassName = "disconnect-button";
        private const string ButtonContainerClassName = ClassName + "__button-container";
        private const string ButtonClassName = ClassName + "__button";
        public Button Button { get; }

        public DisconnectButton()
        {
            AddToClassList(ClassName);

            var buttonContainer = new VisualElement();
            buttonContainer.AddToClassList(ButtonContainerClassName);
            {
                Button = new Button
                {
                    text = BtnLabel,
                };
                Button.AddToClassList(ButtonClassName);
                buttonContainer.Add(Button);
            }

            Add(buttonContainer);
        }

#if !UNITY_2023_1_OR_NEWER
        public new class UxmlFactory : UxmlFactory<DisconnectButton, UxmlTraits>
        {
        }
#endif
    }
}

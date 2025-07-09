namespace Coherence.MatchmakingDialogSample.UI
{
    using UnityEngine.UIElements;

#if UNITY_2023_1_OR_NEWER
    [UxmlElement]
#endif
    public partial class MatchmakingUI : VisualElement
    {
        private const string FindMatchText = "Find Match";

        private const string ClassName = "matchmaking-lobby";

        private const string FindMatchContentLabelClassName = ClassName + "__findmatch-content";
        private const string ButtonsRowContainerClassName = ClassName + "__buttons";

        public MatchmakingOptionsUI Options { get; }

        public Button LogoutButton => Options.LogoutButton;
        public Button FindMatchButton { get; }

        private readonly VisualElement findMatchContent;
        private readonly VisualElement findingMatchButtons;

        public MatchmakingUI()
        {
            AddToClassList(ClassName);

            Options = new MatchmakingOptionsUI();
            Add(Options);

            findMatchContent = new VisualElement();
            findMatchContent.AddToClassList(FindMatchContentLabelClassName);
            {
                findingMatchButtons = new VisualElement();
                findingMatchButtons.AddToClassList(ButtonsRowContainerClassName);
                {
                    FindMatchButton = new Button();
                    FindMatchButton.text = FindMatchText;
                    findingMatchButtons.Add(FindMatchButton);
                }
                findMatchContent.Add(findingMatchButtons);
            }
            Add(findMatchContent);
        }

#if !UNITY_2023_1_OR_NEWER
        public new class UxmlFactory : UxmlFactory<MatchmakingUI, UxmlTraits>
        {
        }
#endif
    }
}

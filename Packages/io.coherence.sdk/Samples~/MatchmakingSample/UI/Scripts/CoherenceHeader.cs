namespace Coherence.MatchmakingDialogSample.UI
{
    using UnityEngine.UIElements;

#if UNITY_2023_1_OR_NEWER
    [UxmlElement]
#endif
    public partial class CoherenceHeader : VisualElement
    {
        private const string ClassName = "coherence-header";
        private const string LogoClassName = "coherence-logo";

        public CoherenceHeader()
        {
            AddToClassList(ClassName);
            var logo = new Image();
            logo.AddToClassList(LogoClassName);
            Add(logo);
        }
    }
}

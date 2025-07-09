// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Collections.Generic;
    using UnityEngine;

    public class LinksModule : HubModule
    {
        public override string ModuleName => "Links";

        private struct LinkData
        {
            public string Header;
            public GUIContent Description;
            public GUIContent ButtonContent;
            public string UrlIdentifier;
            public bool IsDocsUrl;

            public LinkData(string Header, string Description, GUIContent content, string urlIdentifier, bool isDocsUrl = false)
            {
                this.Header = Header;
                this.Description = new GUIContent(Description);
                ButtonContent = content;
                UrlIdentifier = urlIdentifier;
                IsDocsUrl = isDocsUrl;
            }
        }

        private static List<LinkData> links = new List<LinkData>()
        {
            new LinkData("Documentation",
                    "Learn how to use coherence by reading our documentation.",
                    new GUIContent("Open coherence Documentation"),
                    string.Empty, true),

            new LinkData("Dashboard",
                    "The Dashboard lets you configure organizations, projects, worlds, rooms, game services and game sharing.",
                    new GUIContent("Open Dashboard"),
                    ExternalLinks.PortalUrl),

            new LinkData("Discord",
                    "Join our Discord community and meet other people using coherence, as well as our development team.",
                    new GUIContent("Open Discord"),
                    ExternalLinks.Discord),

            new LinkData("Blog",
                    "Keep up to date with latest news on the development of coherence.",
                    new GUIContent("Open Blog"),
                    ExternalLinks.Blog),

            new LinkData("Community",
                    "Engage with the community to learn more, or teach others.",
                    new GUIContent("Open Community Site"),
                    ExternalLinks.Community)
        };

        public override void OnGUI()
        {
            foreach (var item in links)
            {
                CoherenceHubLayout.DrawSection(item.Header, () => DrawLinkSection(item));
            }
        }

        private void DrawLinkSection(LinkData linkData)
        {
            CoherenceHubLayout.DrawInfoLabel(linkData.Description);
            if (CoherenceHubLayout.DrawButton(linkData.ButtonContent))
            {
                var url = linkData.IsDocsUrl
                    ? DocumentationLinks.GetDocsUrl()
                    : linkData.UrlIdentifier;

                Application.OpenURL(url);
            }
        }
    }
}

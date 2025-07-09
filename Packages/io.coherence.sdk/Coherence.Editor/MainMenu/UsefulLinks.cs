// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;

    public class UsefulLinks
    {
        internal static void DeveloperPortal()
        {
            Application.OpenURL(Endpoints.portalUrl);
        }

        internal static void Documentation()
        {
            Application.OpenURL(DocumentationLinks.GetDocsUrl());
        }

        internal static void Discord()
        {
            Application.OpenURL(ExternalLinks.Discord);
        }

        internal static void CommunityForum()
        {
            Application.OpenURL(ExternalLinks.Community);
        }

        internal static void Support()
        {
            Application.OpenURL(ExternalLinks.Support);
        }
    }
}

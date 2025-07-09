// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System;
    using System.Collections.Generic;

    internal static class DocumentationLinks
    {
        public static IEnumerable<DocumentationKeys> ActiveKeys => documentationLinks.Keys;

        // NOTE ending links with a '/' character triggers an additional a redirect that can make tests fail
        private static Dictionary<DocumentationKeys, string> documentationLinks = new()
        {
            { DocumentationKeys.DeveloperPortalOverview, "/hosting/coherence-cloud/online-dashboard" },
            { DocumentationKeys.ProjectSetup, "/getting-started/setup-a-project" },
            { DocumentationKeys.UploadSchema, "/getting-started/setup-a-project/test-in-the-cloud/deploy-replication-server#upload-schema" },
            { DocumentationKeys.PrefabSetup, "/getting-started/setup-a-project/prefab-setup" },
            { DocumentationKeys.SceneSetup, "/getting-started/setup-a-project/scene-setup" },
            { DocumentationKeys.Baking, "/manual/baking-and-code-generation" },
            { DocumentationKeys.Simulators, "/manual/simulation-server" },
            { DocumentationKeys.LocalServers, "/getting-started/setup-a-project/local-development" },
            { DocumentationKeys.AddBridge, "/getting-started/setup-a-project/scene-setup#id-1.-add-a-coherencebridge" },
            { DocumentationKeys.AddLiveQuery, "/getting-started/setup-a-project/scene-setup#id-2.-add-a-livequery" },
            { DocumentationKeys.Schemas, "/manual/advanced-topics/schema-explained" },
            { DocumentationKeys.RoomsAndWorlds, "/manual/replication-server/rooms-and-worlds" },
            { DocumentationKeys.CoherenceBridge, "/manual/components/coherence-bridge" },
            { DocumentationKeys.OnLiveQuerySynced, "/manual/components/coherence-bridge#onlivequerysynced" },
            { DocumentationKeys.CloudService, "/hosting/coherence-cloud" },
            { DocumentationKeys.SimFrame, "/manual/advanced-topics/competitive-games/simulation-frame" },
            { DocumentationKeys.ClientMessages, "/manual/client-connections" },
            { DocumentationKeys.ClientConnectionPrefabs, "/manual/client-connections#clientconnection-objects" },
            { DocumentationKeys.LiveQuery, "/manual/components/coherence-live-query" },
            { DocumentationKeys.Authority, "/manual/authority" },
            { DocumentationKeys.InputQueues, "/manual/authority/server-authoritative-setup" },
            { DocumentationKeys.CoherenceSync, "/manual/components/coherence-sync" },
            { DocumentationKeys.TagQuery, "/manual/components/coherence-tag-query" },
            { DocumentationKeys.SceneTransitioning, "/manual/scenes" },
            { DocumentationKeys.UnlockToken, "/manual/replication-server#unlock-token" },
            { DocumentationKeys.ReleaseNotes, "/support/release-notes" },
            { DocumentationKeys.Parenting, "/manual/parenting-network-entities" },
            { DocumentationKeys.GettingStarted, "/getting-started/setup-a-project" },
            { DocumentationKeys.AutoSimulatorConnection, "/manual/simulation-server/client-vs-simulator-logic#connecting-simulators-automatically-to-rs-autosimulatorconnection-component" },
            { DocumentationKeys.Overview, "/overview" },
            { DocumentationKeys.MaxQueryCount, "/manual/replication-server#maximum-query-count-per-client" },
            { DocumentationKeys.CloudApi, "/hosting/coherence-cloud/coherence-cloud-apis" },
            { DocumentationKeys.ReplicationServerApi, "/manual/replication-server/replication-server-api" },
            { DocumentationKeys.GlobalQuery, "/manual/components/coherenceglobalquery" },
            { DocumentationKeys.ConnectionValidation, "/manual/simulation-server/advanced-simulator-authority#validating-client-connections" },
            { DocumentationKeys.PlayerAccounts, "/hosting/coherence-cloud/authentication-service-player-accounts" },
            { DocumentationKeys.CoherenceScene, "/manual/multiple-connections-within-a-game-instance" },
            { DocumentationKeys.CoherenceSceneLoader, "/manual/multiple-connections-within-a-game-instance" },
            { DocumentationKeys.CoherenceNode, "/manual/components/coherence-node" },
        };

        public static string GetDocsUrl(DocumentationKeys key = DocumentationKeys.None)
        {
            var path = string.Empty;

            if (key != DocumentationKeys.None && !documentationLinks.TryGetValue(key, out path))
            {
                throw new ArgumentException($"Key {key} not registered. Register it in '{nameof(DocumentationLinks)}.{nameof(documentationLinks)}'.", nameof(key));
            }

            return GetDocsBaseUrl() + path;
        }

        private static string GetDocsBaseUrl()
        {
            var settings = RuntimeSettings.Instance;
            var version = settings && settings.VersionInfo != null
                ? "/v/" + settings.VersionInfo.DocsSlug
                : string.Empty;
            return "https://docs.coherence.io" + version;
        }

        private static string GetUnpublishedDocsBaseUrl()
        {
            var settings = RuntimeSettings.Instance;
            var version = settings && settings.VersionInfo != null
                ? "/" + settings.VersionInfo.DocsSlug
                : string.Empty;
            return "https://docs-coherence.gitbook.io" + version;
        }
    }
}

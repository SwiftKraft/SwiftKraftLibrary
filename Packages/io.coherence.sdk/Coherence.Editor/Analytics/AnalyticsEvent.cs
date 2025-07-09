// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal static partial class Analytics
    {
        public static class Events
        {
            public const string Bake = "bake";
            public const string Build = "build";
            public const string CoherenceSyncConfigure = "coherencesync_configure";
            public const string ComponentAdded = "component_added";
            public const string EditorStarted = "editor_started";
            public const string Error = "error";
            public const string HubSectionClicked = "hub_section_clicked";
            public const string RunLocalReplicatorRooms = "run_local_repl_rooms";
            public const string RunLocalReplicatorWorlds = "run_local_repl_worlds";
            public const string SdkInstalled = "sdk_installed";
            public const string SdkLinkedWithPortal = "sdk_linked_with_portal";
            public const string SdkUpdated = "sdk_updated";
            public const string UploadStart = "upload_start";
            public const string UploadEnd = "upload_end";
            public const string UploadSchema = "upload_schema";
            public const string UploadSimStart = "upload_sim_start";
            public const string UploadSimEnd = "upload_sim_end";
            public const string WelcomeScreenButtonClicked = "welcome_screen_button_clicked";
            public const string CoherenceSyncEditor = "coherence_sync_editor";
            public const string Login = "sdk_login";
            public const string MenuItem = "menu_item";
            public const string VsaReport = "vsa_report";
        }

        [Serializable]
        public class Event<T> where T : BaseProperties
        {
#pragma warning disable CS0414
            [SerializeField, JsonProperty("api_key")]
            private string apiKey;

            [JsonProperty("event")]
            private string name;
            [JsonProperty("properties")]
            private T properties;
#pragma warning restore CS0414

            public Event(string name, T properties)
            {
                this.apiKey = projectAPIKey;
                this.name = name;
                this.properties = properties;
            }
        }

        private class GenericProperties : BaseProperties
        {
            [JsonExtensionData(WriteData = true, ReadData = false)]
            public readonly Dictionary<string, JToken> Properties = new();
        }

        [Serializable]
        public class BaseProperties
        {
            [SerializeField, JsonProperty] protected string distinct_id;

#pragma warning disable CS0414
            [SerializeField, JsonProperty("$device_id")] private string device_id;
            [SerializeField, JsonProperty("$insert_id")] private string insert_id;
            [SerializeField, JsonProperty("$os")] private string os;
            [SerializeField, JsonProperty("$os_version")] private string os_version;
            [SerializeField, JsonProperty("$lib")] private string lib;
            [SerializeField, JsonProperty("$lib_version")] private string lib_version;
            [SerializeField, JsonProperty("$session_id")] private string session_id;
            [SerializeField, JsonProperty("$time")] private long time;
            [SerializeField, JsonProperty("$user_id")] private string user_id = null;
            [SerializeField, JsonProperty("$groups")] private GroupProperties groups;

            [SerializeField, JsonProperty] private string coherence_engine_version;
            [SerializeField, JsonProperty] private string coherence_sdk_version;
            [SerializeField, JsonProperty] private string game_engine_version;
            [SerializeField, JsonProperty] private string project_id = null;
            [SerializeField, JsonProperty] private string org_id = null;
#pragma warning restore CS0414

            public BaseProperties()
            {
                var rt = RuntimeSettings;
                var ver = rt != null ? rt.VersionInfo : null;
                var userId = UserID;

                distinct_id = DistinctID;
                device_id = DeviceID;
                insert_id = Guid.NewGuid().ToString();
                os = OS;
                os_version = SystemInfo.operatingSystem;
                lib = EventSource;
                lib_version = "1.0.0";
                session_id = SessionID;
                time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                if (!string.IsNullOrEmpty(userId) && distinct_id == userId)
                {
                    user_id = userId;
                }

                coherence_engine_version = ver != null ? ver.Engine : string.Empty;
                coherence_sdk_version = ver != null ? ver.Sdk : string.Empty;
                game_engine_version = Application.unityVersion;
                if (rt != null && !string.IsNullOrEmpty(rt.ProjectID))
                {
                    project_id = rt.ProjectID;
                }
                if (rt != null && !string.IsNullOrEmpty(rt.OrganizationID))
                {
                    org_id = rt.OrganizationID; // DEPRECATED: remove this property once we fully migrate to groups
                    groups.OrgID = rt.OrganizationID;
                }
            }
        }

        private struct UserProperties
        {
            [JsonProperty("internal_user")]
            public bool InternalUser;
        }

        private struct GroupProperties
        {
            [JsonProperty("organization")]
            public string OrgID;
        }

        private struct OrgProperties
        {
            [JsonProperty("slug")]
            public string Slug;
            [JsonProperty("name")]
            public string Name;
        }

        private class IdentityProperties : BaseProperties
        {
            [JsonProperty("$anon_distinct_id")]
            public string AnonDistinctID;
            [JsonProperty("$set")]
            public UserProperties UserProperties;
        }

        private class OrgIdentityProperties : BaseProperties
        {
            [JsonProperty("$group_type")]
            public string GroupType;
            [JsonProperty("$group_key")]
            public string GroupKey;
            [JsonProperty("$group_set")]
            public OrgProperties GroupSet;

            public OrgIdentityProperties(string orgId, string slug, string name)
            {
                distinct_id = orgId;
                GroupType = "organization";
                GroupKey = orgId;
                GroupSet.Name = name;
                GroupSet.Slug = slug;
            }
        }
    }
}

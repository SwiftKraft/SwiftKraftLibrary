// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using UnityEditor;
    using UnityEngine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Utils;
    using Portal;

    internal static partial class Analytics
    {
        private const string endpoint = "https://xp.coherence.io/capture/";
        private const string projectAPIKey = "phc_OWjEpeOs7PXoRMndJC5cdA7yNf9flI5bDAecZXM5mlD";
        private const string distinctIDPrefKey = "Coherence.Xp.DistinctID";

        static Analytics()
        {
            var userID = UserID;
            var distinctID = DistinctID;

            if (!string.IsNullOrEmpty(userID) && userID != distinctID)
            {
                DistinctID = userID;
            }
        }

        public static string DistinctID
        {
            get
            {
                var uid = EditorPrefs.GetString(distinctIDPrefKey, null);
                if (!string.IsNullOrEmpty(uid))
                {
                    return uid;
                }
                else
                {
                    uid = Guid.NewGuid().ToString();
                    DistinctID = uid;
                    return uid;
                }
            }
            internal set => EditorPrefs.SetString(distinctIDPrefKey, value);
        }

        private static string UserID
        {
            get => ProjectSettings.instance.UserID;
        }

        private static string DeviceID
        {
            get => SystemInfo.deviceUniqueIdentifier.ToLower();
        }

        private static readonly string SessionID = Guid.NewGuid().ToString();

        private static string OS
        {
            get
            {
#if UNITY_EDITOR_OSX
                return "Mac OS X";
#elif UNITY_EDITOR_WIN
                return "Windows";
#else
                return "Linux";
#endif
            }
        }

        private static string EventSource
        {
            get
            {
                return Application.isBatchMode ? "unity-headless" : "unity";
            }
        }

        private static RuntimeSettings RuntimeSettings
        {
            get => ProjectSettings.instance.RuntimeSettings;
        }

        private static bool OptOut
        {
            get => !ProjectSettings.instance.reportAnalytics;
        }

        public static void Identify(string userID, string email)
        {
            var oldDistinctID = DistinctID;
            var newDistinctID = userID;

            if (string.IsNullOrEmpty(newDistinctID) || newDistinctID == oldDistinctID)
            {
                return;
            }

            Guid g;
            var anonymous = Guid.TryParse(oldDistinctID, out g);

            if (anonymous)
            {
                DistinctID = newDistinctID;
                Capture(new Event<IdentityProperties>(
                    "$identify",
                    new IdentityProperties{
                        AnonDistinctID = oldDistinctID,
                        UserProperties = {
                            InternalUser = email.EndsWith("@coherence.io")
                        }
                    }
                ));
            }
            else
            {
                DistinctID = newDistinctID;
            }
        }

        public static void OrgIdentify(Organization org)
        {
            if (org != null)
            {
                Capture(new Event<OrgIdentityProperties>(
                    "$groupidentify",
                    new OrgIdentityProperties(org.id, org.slug, org.name)
                ));
            }
        }

        public static void ResetIdentity()
        {
            DistinctID = Guid.NewGuid().ToString();
        }

        public static void Capture(string eventName)
        {
            var evt = new Event<BaseProperties>(eventName, new BaseProperties());
            Capture(evt);
        }

        public static void Capture(string eventName, params (string key, JToken value)[] properties)
        {
            var eventProperties = new GenericProperties();
            foreach (var prop in properties)
            {
                eventProperties.Properties[prop.key] = prop.value;
            }
            var evt = new Event<GenericProperties>(eventName, eventProperties);
            Capture(evt);
        }

        public static void Capture<T>(Event<T> evt) where T : BaseProperties
        {
            if (OptOut)
            {
                return;
            }

            var payload = CoherenceJson.SerializeObject(
                evt,
                Formatting.None,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }
            );

            _ = Task.Run(async () =>
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(endpoint);
                    req.Method = "POST";
                    req.ContentType = "application/json";

                    using (var streamWriter = new StreamWriter(await req.GetRequestStreamAsync()))
                    {
                        streamWriter.Write(payload);
                    }

                    var res = await req.GetResponseAsync();

                    res.Close();
                }
#if COHERENCE_DEBUG_EDITOR
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
#else
                catch
                {
                }
#endif
            });
        }

    }
}

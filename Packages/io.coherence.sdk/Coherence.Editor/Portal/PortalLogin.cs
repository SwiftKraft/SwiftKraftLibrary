// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using Connection;
    using System.Collections.Generic;
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Threading.Tasks;
    using Log;
    using Toolkit;

    [Serializable]
    internal class PortalLogin
    {
        private class LoginEventProperties : Analytics.BaseProperties
        {
            public string source;
        }

        private const int checkIntervalSeconds = 1; // seconds
        private const int maxAttempts = 300; // 5 minutes
        private static int currentAttempts = 0;

        public static readonly GUIContent orgLabel = EditorGUIUtility.TrTextContent("Organization");

        public static string Nonce = Guid.NewGuid().ToString();
        public static string SdkConnectEndpoint => $"/sdk-connections/{Nonce}?analyticsIdent={Analytics.DistinctID}";
        public static string LoginUrl => $"{Endpoints.LoginBaseURL}/sdk-connect?sdkIdent={Nonce}&analyticsIdent={Analytics.DistinctID}";

        public static Organization[] organizations => fetchedOrganizations;
        public static GUIContent[] organizationPopupContents = new GUIContent[] { new GUIContent("None") };
        public static OrganizationProjectsContent[] organizationProjectsContent = new OrganizationProjectsContent[] { };
        public static List<string> availableRegionsForCurrentProject = new List<string>()
        {
            EndpointData.LocalRegion
        };


        private static bool isPolling = false;
        public static bool IsPolling => isPolling;

        public static bool IsLoggedIn => !string.IsNullOrEmpty(ProjectSettings.instance.LoginToken) && !IsPolling;

        /// <summary>
        /// Gets a value indicating whether data for all available <see cref="organizations"/> has been fetched from
        /// the coherence Developer Portal.
        /// <para>
        /// If <see langword="false"/>, then <see cref="organizations"/> contains an empty array.
        /// </para>
        /// </summary>
        public static bool OrganizationsFetched { get; private set; }

        private static Organization[] fetchedOrganizations = { };
        private static OrganizationSubscription organizationSubscription;

        public static event Action OnLoggedIn;
        public static event Action OnLoggedOut;
        public static event Action OnProjectChanged;

        private static Coherence.Log.Logger Logger = Log.GetLogger<PortalLogin>();

        [Serializable]
        public struct OrganizationProjectsContent
        {
            public GUIContent[] projectContents;
        }

#pragma warning disable 649
        public string id;
        public string name;
        public string email;
        public string token;
#pragma warning restore 649

        public static void RefreshNonce()
        {
            Nonce = Guid.NewGuid().ToString();
        }

        public static void Login(Action onLogin)
        {
            StartSdkConnection(() =>
            {
                OpenBrowserAndLogin(onLogin);
            });
        }

        public static void BeginPolling(Action onLogin)
        {
            if (isPolling)
            {
                return;
            }
            currentAttempts = 0;

            isPolling = true;

            _ = PollForStatus(onLogin);
        }

        public static void StopPolling() => currentAttempts = maxAttempts;

        public static async Task PollForStatus(Action onLogin)
        {
            for (currentAttempts = 0; currentAttempts < maxAttempts; currentAttempts++)
            {
                var loginInfo = Fetch();
                if (loginInfo != null && !string.IsNullOrEmpty(loginInfo.token))
                {
                    ProjectSettings.instance.LoginToken = loginInfo.token;
                    ProjectSettings.instance.UserID = loginInfo.id;
                    ProjectSettings.instance.Email = loginInfo.email;
                    FetchOrgs(orgList =>
                    {
                        if (orgList.orgs != null && orgList.orgs.Length > 0)
                        {
                            var tempOrg = orgList.orgs.FirstOrDefault(org => org.id == RuntimeSettings.Instance.OrganizationID);
                            var newCurrentOrg = tempOrg ?? fetchedOrganizations.FirstOrDefault();

                            AssociateOrganization(newCurrentOrg);
                            _ = OrganizationSubscription.FetchAsync(newCurrentOrg.id, subscription =>
                            {
                                organizationSubscription = subscription;
#if VSP
                                if (subscription.paid_tier)
                                {
                                    VSAttribution.SendAttributionEvent("Login", "coherence", loginInfo.id);
                                    Analytics.Capture(Analytics.Events.VsaReport, ("user_id", loginInfo.id));
                                }
#endif
                            });
                        }
                    });

                    Analytics.Identify(loginInfo.id, loginInfo.email);
                    Analytics.Capture(Analytics.Events.SdkLinkedWithPortal);
                    _ = OrganizationSubscription.FetchAsync(ProjectSettings.instance.RuntimeSettings.OrganizationID,
                    subscription =>
                    {
                        organizationSubscription = subscription;
#if VSP
                        if (subscription.product_name != "Free")
                        {
                            VSAttribution.SendAttributionEvent("Login", "coherence", loginInfo.id);
                        }
#endif
                        Analytics.Capture(
                            new Analytics.Event<LoginEventProperties>(Analytics.Events.Login,
                            new LoginEventProperties
                            {
                                source =
                                    #if VSP
                                        "asset_store",
                                    #else
                                        "registry",
                                    #endif
                            }));
                    });

                    break;
                }

                await Task.Delay(checkIntervalSeconds * 1000);
            }
            isPolling = false;

            if (IsLoggedIn)
            {
                onLogin?.Invoke();
                OnLoggedIn?.Invoke();
            }
        }

        public static async void FetchOrgs(Action<OrganizationList> onComplete = null)
        {
            var orgList = await OrganizationList.Fetch();
            if (orgList.orgs != null)
            {
                List<string> existingProjectIds = new();
                fetchedOrganizations = orgList.orgs;
                OrganizationsFetched = true;
                organizationPopupContents = new GUIContent[organizations.Length + 1];
                organizationProjectsContent = new OrganizationProjectsContent[organizations.Length + 1];
                organizationPopupContents[0] = new GUIContent("None");
                organizationProjectsContent[0].projectContents = null;

                for (var i = 0; i < organizations.Length; i++)
                {
                    var org = organizations[i];

                    organizationPopupContents[i + 1] = new GUIContent(org.name, $"id: {org.id}");
                    var projContents = new GUIContent[org.projects.Length + 1];
                    projContents[0] = new GUIContent("None");
                    for (var j = 0; j < org.projects.Length; j++)
                    {
                        var proj = org.projects[j];
                        projContents[j + 1] = new GUIContent(proj.name, $"id: {proj.id}");
                        existingProjectIds.Add(proj.id);

                        if (RuntimeSettings.Instance.ProjectID == proj.id)
                        {
                            ProjectSimulatorSlugStore.Set(proj.id, RuntimeSettings.Instance.SimulatorSlug);
                        }
                    }
                    organizationProjectsContent[i + 1].projectContents = projContents;
                }

                ProjectSimulatorSlugStore.KeepOnly(key => existingProjectIds.Contains(key));
            }
            else
            {
                DiscardFetchedOrganizations();
            }
            onComplete?.Invoke(orgList);
        }

        public static PortalLogin Fetch()
        {
            PortalRequest req = new PortalRequest($"{Endpoints.sdkConnectPath}/{Nonce}", "GET");
            req.downloadHandler = new DownloadHandlerBuffer();

            _ = req.SendWebRequest();
            while (!req.isDone)
            {
                // do nothing;
            }
            EditorUtility.ClearProgressBar();

            switch (req.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Logger.Warning(Warning.EditorPortalFetchLogin,
                        $"Error portal login: {req.error}");
                    return null;
            }

            if (string.IsNullOrEmpty(req.downloadHandler.text))
            {
                return null;
            }

            try
            {
                var res = JsonUtility.FromJson<PortalLogin>(req.downloadHandler.text);
                return res;
            }
            catch (Exception e)
            {
                Logger.Warning(Warning.EditorPortalFetchParsingSDK,
                    $"Error parsing the SDK connect response: exception={e}, text={req.downloadHandler.text}");
            }
            return null;
        }

        public static void AssociateOrganization(Organization org)
        {
            var runtimeSettings = ProjectSettings.instance.RuntimeSettings;

            if (org == null)
            {
                runtimeSettings.OrganizationID = string.Empty;
                runtimeSettings.OrganizationName = string.Empty;
                AssociateProject(null, Schemas.UpdateSyncState);
                SaveRuntimeSettings(runtimeSettings);
                return;
            }

            if (org.id.Equals(runtimeSettings.OrganizationID))
            {
                return;
            }

            runtimeSettings.OrganizationID = org.id;
            runtimeSettings.OrganizationName = org.name;
            SaveRuntimeSettings(runtimeSettings);
            AssociateProject(null, Schemas.UpdateSyncState);
            Analytics.OrgIdentify(org);
        }

        public static void AssociateProject(ProjectInfo proj, Action onCompleted = null)
        {
            var rt = ProjectSettings.instance.RuntimeSettings;
            if (proj == null)
            {
                if (rt)
                {
                    rt.ProjectID = null;
                    rt.ProjectName = null;
                    rt.RuntimeKey = null;
                    rt.SimulatorSlug = null;
                    availableRegionsForCurrentProject = new List<string>() { EndpointData.LocalRegion };
                }

                SaveRuntimeSettings(rt);

                onCompleted?.Invoke();
                OnProjectChanged?.Invoke();

                return;
            }

            rt.ProjectID = proj.id;
            rt.ProjectName = proj.name;
            rt.RuntimeKey = proj.runtime_key;
            rt.SimulatorSlug = ProjectSimulatorSlugStore.Get(proj.id);

            SaveRuntimeSettings(rt);
            FetchRegionsForProject(proj.id);

            onCompleted?.Invoke();
            OnProjectChanged?.Invoke();
        }

        public static void Logout()
        {
            UnityWebRequest.ClearCookieCache();
            DiscardFetchedOrganizations();
            ProjectSettings.instance.LoginToken = null;
            ProjectSettings.instance.UserID = null;
            ProjectSettings.instance.Email = null;
            organizationSubscription = null;
            Analytics.ResetIdentity();
            RefreshNonce();

            OnLoggedOut?.Invoke();
        }

        /// <summary>
        /// Sets <see cref="organizations"/> to a zero-sized array
        /// and <see cref="OrganizationsFetched"/> to <see langword="false"/>.
        /// </summary>
        internal static void DiscardFetchedOrganizations()
        {
            fetchedOrganizations = new Organization[] { };
            OrganizationsFetched = false;
        }

        /// <summary>
        /// Sets <see cref="organizations"/> to the given value,
        /// and <see cref="OrganizationsFetched"/> to <see langword="true"/>.
        /// </summary>
        internal static void SetOrganizations(Organization[] organizations)
        {
            fetchedOrganizations = organizations;
            OrganizationsFetched = true;
        }

        private static async void FetchRegionsForProject(string projectId)
        {
            var regions = await RegionsList.Fetch(projectId);

            availableRegionsForCurrentProject.Clear();
            availableRegionsForCurrentProject.Add(EndpointData.LocalRegion);
            availableRegionsForCurrentProject.AddRange(regions.regions);
        }

        private static void StartSdkConnection(Action onSuccess)
        {
            PortalRequest req = new PortalRequest(SdkConnectEndpoint, "POST");
            req.downloadHandler = new DownloadHandlerBuffer();

            _ = req.SendWebRequest();
            while (!req.isDone)
            {
                EditorUtility.DisplayProgressBar("Connecting..", "Opening connection with coherence", 0f);
            }

            EditorUtility.ClearProgressBar();

            switch (req.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Logger.Warning(Warning.EditorPortalStartLogin,
                        $"Error portal login: {req.error}");
                    return;
            }

            onSuccess.Invoke();
        }

        private static void OpenBrowserAndLogin(Action onLogin)
        {
            Application.OpenURL(LoginUrl);
            BeginPolling(onLogin);
        }

        /// <summary>
        /// Validates organization id, if data for all available organizations has previously been fetched from the portal.
        /// </summary>
        /// <param name="organizationID"> Organization ID to validate. </param>
        /// <param name="isValid">
        /// <see langword="true"/> if data for all available organizations has been fetched for the user,
        /// and <paramref name="organizationID"/>  was found among the ids of the available organizations;
        /// otherwise, <see langword="false"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="organizationID"/> was validated using data of
        /// all available organizations which had previously been fetched from the portal;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryValidateOrganizationID(string organizationID, out bool isValid)
        {
            if (!OrganizationsFetched)
            {
                isValid = false;
                return false;
            }

            foreach (var organization in organizations)
            {
                if (string.Equals(organizationID, organization.id))
                {
                    isValid = true;
                    return true;
                }
            }

            isValid = false;
            return true;
        }

        private static void SaveRuntimeSettings(RuntimeSettings runtimeSettings)
        {
            EditorUtility.SetDirty(runtimeSettings);
            AssetDatabase.SaveAssetIfDirty(runtimeSettings);
        }
    }
}

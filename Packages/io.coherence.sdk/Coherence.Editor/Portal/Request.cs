// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using Connection;
    using Log;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;
    using Logger = Log.Logger;
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;
    using System.Linq;

    internal class PortalRequest : UnityWebRequest
    {
        private const string RequestIDHeader = "X-Coherence-Request-ID";
        private const string ProjectDeletedHeader = "X-Coherence-Project-Deleted";
        private const string CreditLimitExceededHeader = "X-Coherence-Credit-Limit-Exceeded";
        private const string MinVersionHeader = "X-Coherence-Client-Minimum-Version";

        private static readonly RequestIdSource IdSource = new RequestIdSource();
        private static readonly Lazy<Logger> Logger = new Lazy<Logger>(() => Log.GetLogger<PortalRequest>());

        private UnityWebRequestAsyncOperation op;
        private bool async;
        private readonly string requestId;
        private readonly string path;
        private readonly Dictionary<string, string> headers = new Dictionary<string, string>();

        internal static bool TryCreate(string path, string organizationID, string projectID, string method, bool willWaitAsync, [NotNullWhen(true), MaybeNullWhen(false)] out PortalRequest request, [NotNullWhen(false), MaybeNullWhen(true)] out string error)
        {
            if (Endpoints.TryGet(path, organizationID, projectID, out var pathEndpoint, out error))
            {
                request = new PortalRequest(path, method, willWaitAsync, Endpoints.Portal + pathEndpoint);
                error = null;
                return true;
            }

            request = default;
            return false;
        }

        public PortalRequest(string path, string method, bool willWaitAsync = false) : this(path, method, willWaitAsync, Endpoints.Portal + Endpoints.Get(path)) { }

        private PortalRequest(string path, string method, bool willWaitAsync, string url) : base(url, method)
        {
            this.path = path;
            async = willWaitAsync;
            requestId = IdSource.Next();

            PackageInfo packageInfo = PackageInfo.FindForAssetPath(Paths.packageManifestPath);

            if (!string.IsNullOrEmpty(ProjectSettings.instance.LoginToken))
            {
                SetRequestHeader("X-Coherence-Sdk-Token", ProjectSettings.instance.LoginToken);
            }
            else if (!string.IsNullOrEmpty(ProjectSettings.instance.PortalToken))
            {
                SetRequestHeader("X-Coherence-Portal-Token", ProjectSettings.instance.PortalToken);
            }

            if (!string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.OrganizationID))
            {
                SetRequestHeader("X-Coherence-Organization-Id", ProjectSettings.instance.RuntimeSettings.OrganizationID);
            }

            if (!string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID))
            {
                SetRequestHeader("X-Coherence-Project-Id", ProjectSettings.instance.RuntimeSettings.ProjectID);
            }

            SetRequestHeader("X-Coherence-Client", "unity-sdk-v" + packageInfo.version);
            SetRequestHeader(RequestIDHeader, requestId);

            if (method == "POST")
            {
                SetRequestHeader("Content-Type", "application/json");
            }
        }

        public new void SetRequestHeader(string name, string value)
        {
            headers.Add(name, value);
            base.SetRequestHeader(name, value);
        }

        public UnityWebRequestAsyncOperation SendWebRequest(Action<AsyncOperation> callback = null)
        {
            Logger.Value.Debug($"Request", ("requestID", requestId),
                ("path", path), ("method", method),
                ProjectSettings.instance.UseCustomEndpoints ? ("endpoint", Endpoints.Portal) : ("", ""),
                ("headers", $"[{string.Join(", ", headers?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>())}]"));

            op = base.SendWebRequest();
            if (callback != null)
            {
                op.completed += callback;
            }

            if (!async)
            {
                op.completed += done;
            }

            return op;
        }

        private void done(AsyncOperation op)
        {
            try
            {
                LogResult();

                var projDeleted = GetResponseHeader(ProjectDeletedHeader);
                var creditLimitExceeded = GetResponseHeader(CreditLimitExceededHeader);

                if (!string.IsNullOrEmpty(projDeleted))
                {
                    Logger.Value.Warning(Warning.EditorPortalRequestProjectDeleted);
                    PortalLogin.AssociateProject(null);
                    return;
                }

                if (!string.IsNullOrEmpty(creditLimitExceeded))
                {
                    Logger.Value.Warning(Warning.EditorPortalRequestCreditsExceeded);
                }

                // First check if there's an issue with the version of the SDK we are using.
                var minVersion = GetResponseHeader(MinVersionHeader);
                if (minVersion != null)
                {
                    PackageInfo packageInfo = PackageInfo.FindForAssetPath(Paths.packageManifestPath);
                    if (responseCode == 403)
                    {
                        var message = $"The coherence SDK version you are using ({packageInfo.version}) is no longer supported. Please upgrade to {minVersion} or newer.";
                        Logger.Value.Error(Error.EditorPortalRequestSDKUnsupported, message);
                        _ = EditorUtility.DisplayDialog("Unsupported coherence version", message, "Ok");
                        return;
                    }

                    Logger.Value.Warning(Warning.EditorPortalRequestSDKDeprecated,
                        $"The coherence SDK version you are using ({packageInfo.version}) is deprecated. Please upgrade to {minVersion} or newer.");
                }
                else
                {
                    // 423 here means that the feature wasn't enabled
                    if (responseCode == 423)
                    {
                        var message = "Feature not enabled. Visit your developer portal to enable it.";
                        Logger.Value.Error(Error.EditorPortalRequestFeatureNotSupported, message);
                        _ = EditorUtility.DisplayDialog("Feature not enabled", message, "Ok");
                    }
                    // If you upgrade old project with login info cached it needs to be reset
                    else if (responseCode == 401 && downloadHandler.text.Contains("ERR_TOKEN_MISSING"))
                    {
                        Logger.Value.Warning(Warning.EditorPortalRequestMissingToken);
                    }
                }
            }
            finally
            {
                Dispose();
            }
        }

        private void LogResult()
        {
            if (result == Result.Success)
            {
                Logger.Value.Debug($"Response",
                    ("requestID", requestId),
                    ("responseID", GetResponseHeader(RequestIDHeader)),
                    ("path", path),
                    ("method", method),
                    ("statusCode", responseCode),
                    ProjectSettings.instance.UseCustomEndpoints ? ("endpoint", Endpoints.Portal) : ("", ""),
                    ("body", downloadHandler?.text));
            }
            else
            {
                Logger.Value.Warning(Warning.EditorPortalRequestFailed,
                    $"Request failed",
                    ("requestID", requestId),
                    ("path", path),
                    ("method", method),
                    ("statusCode", responseCode),
                    ("error", error),
                    ("result", result),
                    ProjectSettings.instance.UseCustomEndpoints ? ("endpoint", Endpoints.Portal) : ("", ""),
                    ("body", downloadHandler?.text));
            }
        }
    }
}

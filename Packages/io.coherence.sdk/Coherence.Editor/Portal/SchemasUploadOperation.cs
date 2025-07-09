// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor.Portal
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;

    /// <summary>
    /// Represents an operation for uploading <see cref="Schemas"/> to the coherence Developer Portal.
    /// <note>
    /// Communication with the portal happens through HTTP synchronously.
    /// </note>
    /// </summary>
    internal sealed class SchemasUploadOperation
    {
        /// <summary>
        /// Specifies the different reasons for which an operation to upload schemas to the cloud can fail.
        /// </summary>
        internal enum FailReason
        {
            None,
            AbortedByUser,

            MissingSchemaID,
            MissingRuntimeSettings,
            MissingPortalAndLoginTokens,
            MissingOrganizationID,
            InvalidOrganizationID,
            MissingProjectID,

            ProtocolError,
            ConnectionError,
            DataProcessingError,
            RequestCreationFailed
        }

        /// <summary>
        /// Represents the result of an operation to upload schemas to the cloud.
        /// </summary>
        internal sealed class Result
        {
            public static readonly Result Success = new(FailReason.None, null);
            public static readonly Result AbortedByUser = new(FailReason.AbortedByUser, null);

            private Result(FailReason failReason, string webRequestError)
            {
                FailReason = failReason;
                WebRequestError = webRequestError;
            }

            public FailReason FailReason { get; }

            [MaybeNull]
            public string WebRequestError { get; }

            public static Result Failed(FailReason failReason) => new(failReason, null);
            public static Result ConnectionError([DisallowNull] string error) => new(FailReason.ConnectionError, error);
            public static Result ProtocolError([DisallowNull] string error) => new(FailReason.ProtocolError, error);
            public static Result DataProcessingError([DisallowNull] string error) => new(FailReason.DataProcessingError, error);
            public static Result RequestCreationFailed([DisallowNull] string error) => new(FailReason.RequestCreationFailed, error);
        }

        public SchemasUploadOperation(Schemas schemas, string portalToken, string loginToken, string organizationID, string projectID, string projectName)
        {
            Schemas = schemas;
            PortalToken = portalToken;
            LoginToken = loginToken;
            OrganizationID = organizationID;
            ProjectID = projectID;
            ProjectName = projectName;
        }

        public Schemas Schemas { get; }
        public string PortalToken { get; }
        public string LoginToken { get; }
        public string OrganizationID { get; }
        public string ProjectID { get; }
        public string ProjectName { get; }

        /// <summary>
        /// Attempts to upload the schemas to the cloud.
        /// </summary>
        /// <param name="interactionMode">Indicates whether the user should be prompted for confirmation or performed automatically.</param>
        /// <returns> Object describing the result of the operation. </returns>
        internal Result Upload(InteractionMode interactionMode = InteractionMode.AutomatedAction)
        {
            if (string.IsNullOrEmpty(PortalToken) && string.IsNullOrEmpty(LoginToken))
            {
                return Result.Failed(FailReason.MissingPortalAndLoginTokens);
            }

            if (string.IsNullOrEmpty(OrganizationID))
            {
                return Result.Failed(FailReason.MissingOrganizationID);
            }

            if (PortalLogin.TryValidateOrganizationID(OrganizationID, out var idIsValid) && !idIsValid)
            {
                return Result.Failed(FailReason.InvalidOrganizationID);
            }

            if (string.IsNullOrEmpty(ProjectID))
            {
                return Result.Failed(FailReason.MissingProjectID);
            }

            if (string.IsNullOrEmpty(Schemas.id))
            {
                return Result.Failed(FailReason.MissingSchemaID);
            }

            if (!PortalRequest.TryCreate(Endpoints.schemasPath, OrganizationID, ProjectID, "POST", false, out var portalRequest, out var error))
            {
                return Result.RequestCreationFailed(error);
            }

            if (!Application.isBatchMode && interactionMode == InteractionMode.UserAction
            && !EditorUtility.DisplayDialog("Upload Schemas Now?",
            $"Local Schema ID: {Schemas.id}\nProject Name: {ProjectName}\nProject ID: {ProjectID}", "Upload",
            "Cancel"))
            {
                return Result.AbortedByUser;
            }

            var body = JsonUtility.ToJson(Schemas);
            var bodyRaw = Encoding.UTF8.GetBytes(body);
            portalRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            portalRequest.downloadHandler = new DownloadHandlerBuffer();
            portalRequest.disposeUploadHandlerOnDispose = true;
            portalRequest.disposeDownloadHandlerOnDispose = true;

            _ = portalRequest.SendWebRequest();

            while (!portalRequest.isDone)
            {
                if (!Application.isBatchMode)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Portal", "Uploading schemas...", portalRequest.uploadProgress))
                    {
                        EditorUtility.ClearProgressBar();
                        portalRequest.Abort();
                        return Result.AbortedByUser;
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            switch (portalRequest.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                    return Result.ProtocolError(portalRequest.error);
                case UnityWebRequest.Result.ConnectionError:
                    return Result.ConnectionError(portalRequest.error);
                case UnityWebRequest.Result.DataProcessingError:
                    return Result.DataProcessingError(portalRequest.error);
            }

            _ = ProjectSettings.instance.RehashActiveSchemas();
            Schemas.UpdateSyncState();
            Analytics.Capture(Analytics.Events.UploadSchema);
            return Result.Success;
        }
    }
}

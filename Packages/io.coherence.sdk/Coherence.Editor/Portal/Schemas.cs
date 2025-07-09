// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Log;
    using Newtonsoft.Json;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;
    using Utils;
    using static SchemasUploadOperation;
    using Logger = Log.Logger;

    [Serializable]
    public class Schemas
    {
        public enum SyncState
        {
            Unknown,
            InProgress,
            InSync,
            OutOfSync,
        }



        public static event Action OnSchemaStateUpdate;

        private const string stateKey = "Coherence.Portal.SyncState";
        private const string initKey = "Coherence.Portal.InitializedSyncState";

        private static SyncState? cachedState;
        private static string cachedSchemaId;

        private static readonly Lazy<Logger> logger = new(() => Log.GetLogger(typeof(Schemas)));
        private static Logger Logger => logger.Value;

        private static readonly Dictionary<SyncState, GUIContent> stateContents =
            new()
            {
                {
                    SyncState.Unknown, EditorGUIUtility.TrTextContent(
                                                                      ObjectNames.NicifyVariableName(SyncState.Unknown.ToString()),
                                                                      "You need to log in to the Portal and have a project selected, to be able to sync your local schema.")
                },
                {
                    SyncState.InProgress, EditorGUIUtility.TrTextContent(
                                                                         ObjectNames.NicifyVariableName(SyncState.InProgress.ToString()),
                                                                         string.Empty, "Warning")
                },
                {
                    SyncState.OutOfSync, EditorGUIUtility.TrTextContent(
                                                                        ObjectNames.NicifyVariableName(SyncState.OutOfSync.ToString()),
                                                                        "Your local schema hasn't been uploaded to your current project.", "Warning")
                },
                { SyncState.InSync, EditorGUIUtility.TrTextContent(ObjectNames.NicifyVariableName(SyncState.InSync.ToString())) },
            };

        public static string[] RemoteSchemaIDs { get; private set; } =
            { };

        public static SchemaState[] RemoteSchemaStates { get; private set; } =
            { };

        public static SyncState state
        {
            get => cachedState ??= (SyncState)UserSettings.GetInt(Schemas.stateKey, 0);

            internal set
            {
                if (cachedState == value)
                {
                    return;
                }

                cachedState = value;
                if (value != SyncState.InProgress)
                {
                    // NOTE: Avoid overwriting last known serialized sync state with InProgress.
                    // This way if UpdateSyncState is executed but never finishes for some reason,
                    // our GUIs don't get stuck displaying 'In Progress' as easily (this can happen
                    // often when options like 'Bake On Enter Play Mode' are enabled in Project Settings).
                    UserSettings.SetInt(Schemas.stateKey, (int)value);

                    Schemas.OnSchemaStateUpdate?.Invoke();
                }
            }
        }

        public static GUIContent StateContent => Schemas.stateContents[Schemas.state];

#pragma warning disable 649
        public Schema[] schemas;
        public string id;
#pragma warning restore 649

        static Schemas() => EditorApplication.delayCall += Schemas.InitializeSyncState;

        private static void InitializeSyncState()
        {
            if (!SessionState.GetBool(Schemas.initKey, false))
            {
                Schemas.UpdateSyncState();
                SessionState.SetBool(Schemas.initKey, true);
            }
        }

        internal Schemas(Schema[] schemas, string id)
        {
            this.schemas = schemas;
            this.id = id;
        }

        internal static Schemas FromActiveSchemas() => new(ActiveSchemas(), GetLocalSchemaID());

        private static Schema[] ActiveSchemas()
        {
            var schemas = new List<Schema>();

            if (File.Exists(Paths.toolkitSchemaPath))
            {
                schemas.Add(Schema.GetFromString(File.ReadAllText(Paths.toolkitSchemaPath)));
            }

            if (File.Exists(Paths.gatherSchemaPath))
            {
                schemas.Add(Schema.GetFromString(File.ReadAllText(Paths.gatherSchemaPath)));
            }

            var activeSchemas = ProjectSettings.instance.activeSchemas.Select(Schema.GetFromSchemaAsset);
            schemas.AddRange(activeSchemas);

            if (ProjectSettings.instance.RuntimeSettings.extraSchemas != null)
            {
                var extraSchemas = ProjectSettings.instance.RuntimeSettings.extraSchemas
                .Where(s => s != null)
                .Select(Schema.GetFromSchemaAsset);
                schemas.AddRange(extraSchemas);
            }

            return schemas.ToArray();
        }

        public static string GetLocalSchemaID()
        {
            if (!string.IsNullOrEmpty(Schemas.cachedSchemaId))
            {
                return Schemas.cachedSchemaId;
            }

            Schemas.cachedSchemaId = HashCalc.SHA1Hash(Schemas.GetCombinedSchemaContents());
            return Schemas.cachedSchemaId;
        }

        public static string GetCombinedSchemaContents()
        {
            return string.Join("\n", Schemas.ActiveSchemas().Select(s => s.Contents));
        }

        public static bool UploadActive(InteractionMode interactionMode = InteractionMode.AutomatedAction)
        {
            var schemaID = BakeUtil.SchemaIDShort;
            var projectSettings = ProjectSettings.instance;
            var runtimeSettings = projectSettings.RuntimeSettings;

            Result result;
            if (!runtimeSettings)
            {
                result = Result.Failed(FailReason.MissingRuntimeSettings);
            }
            else
            {
                var schemas = FromActiveSchemas();
                var uploadOperation = new SchemasUploadOperation
                (
                    schemas,
                    projectSettings.PortalToken,
                    projectSettings.LoginToken,
                    runtimeSettings.OrganizationID,
                    runtimeSettings.ProjectID,
                    runtimeSettings.ProjectName
                );

                result = uploadOperation.Upload(interactionMode);
            }

            if (result == Result.Success)
            {
                Schemas.Logger.Info($"Uploaded Schema {schemaID} successfully to Project {runtimeSettings.ProjectName}.");
                return true;
            }

            if (result.FailReason is FailReason.MissingRuntimeSettings or FailReason.MissingSchemaID && interactionMode == InteractionMode.UserAction)
            {
                _ = EditorUtility.DisplayDialog("Can't Upload Schemas", "Either there's no codegen available, or you haven't set up a project.", "Ok");
                return false;
            }

            if (result.WebRequestError is not null)
            {
                Logger.Error(Error.EditorPortalSchemaUploadFail, GetFailMessage(result.FailReason));

                if (!Application.isBatchMode && interactionMode == InteractionMode.UserAction)
                {
                    _ = EditorUtility.DisplayDialog("Schemas upload", "Uploading schemas failed!", "OK");
                }
            }
            else
            {
                Logger.Info(GetFailMessage(result.FailReason));
            }

            return false;

            string GetFailMessage(FailReason failReason) => failReason switch
            {
                FailReason.MissingPortalAndLoginTokens => $"Attempting to upload Schema {schemaID} but no Login Token or Portal Token has been found. Make sure you're logged in to the coherence Cloud in 'coherence > coherence Hub > coherence Cloud'.",
                FailReason.MissingOrganizationID => $"Attempting to upload Schema {schemaID} but no Organization ID has been found. Make sure you've selected an Organization in 'coherence > coherence Hub > coherence Cloud > Account'.",
                FailReason.InvalidOrganizationID => $"Attempting to upload Schema {schemaID} but selected Organization ID {projectSettings.RuntimeSettings.OrganizationID} is not recognized. Make sure you've selected a valid Project in 'coherence > coherence Hub > coherence Cloud > Account'.",
                FailReason.MissingProjectID => $"Attempting to upload Schema {schemaID} but no Project ID has been found. Make sure you've selected a Project in 'coherence > coherence Hub > coherence Cloud > Account'.",
                FailReason.AbortedByUser => "Schema upload was aborted by the user.",
                FailReason.ProtocolError
                    or FailReason.ConnectionError
                    or FailReason.DataProcessingError
                    or FailReason.RequestCreationFailed => $"Failed to upload Schema {schemaID} to Project {projectSettings.RuntimeSettings.ProjectName}: {result.WebRequestError}.",
                FailReason.None
                    or FailReason.MissingRuntimeSettings
                    or FailReason.MissingSchemaID
                    => throw new IndexOutOfRangeException($"Fail reason {failReason} should have already been handled above."),
                _ => throw new IndexOutOfRangeException($"Fail reason {failReason} unknown."),
            };
        }

        /// <summary>
        /// Obsolete. Use <see cref="UploadActive"/> instead.
        /// </summary>
        public static bool UploadSchemas(bool warn = false) => UploadActive(warn ? InteractionMode.UserAction : InteractionMode.AutomatedAction);

        /// <summary>
        /// Obsolete. Use <see cref="UploadActive"/> instead.
        /// </summary>
        public static bool Upload() => UploadActive();

        public static Schemas Get()
        {
            // NOTE base64'd schema data is not retrieved
            var req = new PortalRequest(Endpoints.schemasPath, "GET");
            req.downloadHandler = new DownloadHandlerBuffer();
            _ = req.SendWebRequest();

            if (!Application.isBatchMode)
            {
                while (!req.isDone)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Portal", "Downloading schemas...",
                                                                   req.uploadProgress))
                    {
                        EditorUtility.ClearProgressBar();
                        req.Abort();
                        return null;
                    }
                }

                EditorUtility.ClearProgressBar();
            }

            switch (req.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Schemas.Logger.Info($"Error getting schemas: {req.error}");
                    return null;
            }

            Schemas.Logger.Info(req.downloadHandler.text);
            return JsonUtility.FromJson<Schemas>(req.downloadHandler.text);
        }

        private static UnityWebRequest GetAsyncRaw(Action<AsyncOperation> onCompleted = null)
        {
            // NOTE base64'd schema data is not retrieved

            var req = new PortalRequest(Endpoints.schemasPath, "GET");
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SendWebRequest(onCompleted);

            return req;
        }

        public static void UpdateSyncState()
        {
            if (!PortalUtil.CanCommunicateWithPortal ||
                string.IsNullOrEmpty(ProjectSettings.instance.RuntimeSettings.ProjectID))
            {
                Schemas.state = SyncState.Unknown;
                return;
            }

            Schemas.state = SyncState.InProgress;

            var req = Schemas.GetAsyncRaw(op =>
            {
                var asyncOp = op as UnityWebRequestAsyncOperation;
                Schemas.state = Schemas.GetSyncStateFromRequest(asyncOp.webRequest);
            });

            if (!Application.isBatchMode)
            {
                return;
            }

            while (!req.isDone) { }

            Schemas.state = Schemas.GetSyncStateFromRequest(req);
        }

        private static SyncState GetSyncStateFromRequest(UnityWebRequest req)
        {
            switch (req.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Schemas.Logger.Info(req.error);
                    return SyncState.Unknown;
            }

            var schemas = CoherenceJson.DeserializeObject<SchemaState[]>(req.downloadHandler.text);
            Schemas.RemoteSchemaStates = schemas;
            Schemas.RemoteSchemaIDs = schemas.Select(s => s.Id).ToArray();
            if (schemas.Length == 0)
            {
                return SyncState.Unknown;
            }

            //var hash = BakeUtil.SchemaID;
            var hash = Schemas.GetLocalSchemaID();
            if (!schemas.Any(s => s.Id == hash))
            {
                return SyncState.OutOfSync;
            }

            return SyncState.InSync;
        }

        internal static void InvalidateSchemaCache()
        {
            Schemas.cachedSchemaId = string.Empty;
        }

        internal static SyncState GetSyncStateForSchemaId(string schemaId)
            => RemoteSchemaIDs.Length is 0 ? SyncState.Unknown
            : RemoteSchemaIDs.All(s => s == schemaId)
                ? SyncState.InSync
                : SyncState.OutOfSync;

#pragma warning disable 649

        public struct SchemaState
        {
            [JsonProperty("schema_id")]
            public string Id;

            [JsonProperty("timestamp")]
            public string Timestamp;

            public override string ToString()
            {
                return $"{nameof(SchemaState.Id)}: {Id}," +
                       $"{nameof(SchemaState.Timestamp)}: {Timestamp}";
            }
        }
#pragma warning restore 649
    }
}

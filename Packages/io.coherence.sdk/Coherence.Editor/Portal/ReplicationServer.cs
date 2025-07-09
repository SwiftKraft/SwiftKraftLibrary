// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Linq;
    using System.IO;

    [System.Serializable]
    internal class DeployReplicationServerPayload
    {
        public string engine_version;
    }

    internal class ReplicationServer
    {

        public static bool Restart()
        {
            if (!PortalUtil.CanCommunicateWithPortal)
            {
                return false;
            }

            PortalRequest req = new PortalRequest(Endpoints.restartUrlPath, "POST");


            _ = req.SendWebRequest();

            while (!req.isDone)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Portal", "Restarting the replication server...", req.uploadProgress))
                {
                    EditorUtility.ClearProgressBar();
                    req.Abort();
                    return false;
                }
            }

            EditorUtility.ClearProgressBar();

            switch (req.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.Log(req.error);
                    return false;
            }

            return true;
        }

        public static bool Deploy()
        {
            if (!PortalUtil.CanCommunicateWithPortal)
            {
                return false;
            }

            var engine_version = AssetDatabase.LoadAssetAtPath<VersionInfo>(Paths.versionInfoPath).Engine;
            var payload = new DeployReplicationServerPayload { engine_version = engine_version };
            string body = JsonUtility.ToJson(payload);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);

            PortalRequest req = new PortalRequest(Endpoints.deployUrlPath, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);

            _ = req.SendWebRequest();

            while (!req.isDone)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Portal", "Requesting replication server deployment...", req.uploadProgress))
                {
                    EditorUtility.ClearProgressBar();
                    req.Abort();
                    return false;
                }
            }

            EditorUtility.ClearProgressBar();

            switch (req.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.Log(req.error);
                    return false;
            }
            Debug.Log($"Visit your coherence developer portal to see your replication servers' status at: {Endpoints.Portal}");

            return true;
        }
    }
}

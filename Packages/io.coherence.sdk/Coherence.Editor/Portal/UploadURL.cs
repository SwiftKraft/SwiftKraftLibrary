// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.IO;

    [System.Serializable]
    internal class UploadURL
    {

#pragma warning disable 649
        public string url;
#pragma warning restore 649

        public static UploadURL GetSimulator(long size)
        {
            var settings = ProjectSettings.instance.RuntimeSettings;

            string slug = settings.SimulatorSlug;
            string rsVer = settings.VersionInfo.Engine;
            string schemaId = BakeUtil.SchemaID;

            string encodedSlug = UnityWebRequest.EscapeURL(slug);
            string path = string.Format(Endpoints.simUploadUrlPath, size, encodedSlug, schemaId, rsVer);
            return Get(path);
        }

        public bool RegisterSimulator()
        {
            var settings = ProjectSettings.instance.RuntimeSettings;

            RegisterSimulatorBody body = new RegisterSimulatorBody();
            body.slug = settings.SimulatorSlug;
            body.schema_id = BakeUtil.SchemaID;
            body.rs_version = settings.VersionInfo.Engine;

            PortalRequest req = new PortalRequest(Endpoints.registerSimUrlPath, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.disposeUploadHandlerOnDispose = true;

            _ = req.SendWebRequest();

            while (!req.isDone)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Portal", "Registering simulator...", req.uploadProgress))
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
                    Debug.LogError($"Error registering the simulator: {req.error}");
                    return false;
            }

            Debug.Log("Simulator registered and is ready to be used for Worlds. If you want to use it for Rooms, make sure Simulators are enabled in the coherence Portal.");
            return true;
        }
        public static bool RegisterBuild(string platform, string filename)
        {
            RegisterBuildBody body = new RegisterBuildBody();
            body.platform = platform;
            body.filename = filename;

            PortalRequest req = new PortalRequest(Endpoints.registerBuildUrlPath, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.disposeUploadHandlerOnDispose = true;

            _ = req.SendWebRequest();

            while (!req.isDone)
            {
                if (!Application.isBatchMode && EditorUtility.DisplayCancelableProgressBar("Portal", "Registering build...", req.uploadProgress))
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
                    Debug.LogError($"Error registering the build: {req.error}");
                    return false;
            }

            Debug.Log("Build registered and is ready to be used.");
            return true;
        }
        public static UploadURL GetGame(long size, string platform)
        {
            string path = string.Format(Endpoints.gameUploadUrlPath, platform, size);
            return Get(path);
        }

        public static UploadURL GetWebGLFile(long size, string filename, bool streaming)
        {
            var strContext = streaming ? "streamingAsset" : "game";
            string path = string.Format(Endpoints.webglUploadUrlPath, filename, size, strContext);
            return Get(path);
        }

        private static UploadURL Get(string path)
        {
            PortalRequest req = new PortalRequest(path, "GET");
            req.downloadHandler = new DownloadHandlerBuffer();
            _ = req.SendWebRequest();

            while (!req.isDone)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Portal", $"Requesting upload URL...", req.downloadProgress))
                {
                    EditorUtility.ClearProgressBar();
                    req.Abort();
                    return null;
                }
            }
            EditorUtility.ClearProgressBar();

            switch (req.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError($"Error generating the upload URL. Path={path} Error={req.error} - {req.downloadHandler.text}");
                    return null;
            }

            return JsonUtility.FromJson<UploadURL>(req.downloadHandler.text);
        }

        public bool Upload(string filePath, long size, string message = "Uploading file...")
        {
            if (!PortalUtil.CanCommunicateWithPortal)
            {
                return false;
            }

            byte[] data = File.ReadAllBytes(filePath);
            using (var req = UnityWebRequest.Put(url, data))
            {
                req.disposeUploadHandlerOnDispose = true;
                foreach (KeyValuePair<string, string> keyValuePair in getHeaders(filePath))
                {
                    req.SetRequestHeader(keyValuePair.Key, keyValuePair.Value);
                }

                _ = req.SendWebRequest();

                while (!req.isDone)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Portal", message, req.uploadProgress))
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
                        Debug.LogError($"Error uploading the build: {req.error}");
                        return false;
                }

                return true;
            }
        }

        private Dictionary<string, string> getHeaders(string filename)
        {
            var map = new Dictionary<string, string>();
            if (filename.Contains(".js"))
            {
                map.Add("Content-Type", "application/javascript");
            }
            else if (filename.Contains(".wasm"))
            {
                map.Add("Content-Type", "application/wasm");
            }
            else if (filename.Contains(".data"))
            {
                map.Add("Content-Type", "octet-stream");
            }
            else if (filename.Contains(".symbols.json"))
            {
                map.Add("Content-Type", "octet-stream");
            }

            if (filename.Contains(".br"))
            {
                map.Add("Content-Encoding", "br");
            }
            else if (filename.Contains(".gz"))
            {
                map.Add("Content-Encoding", "gzip");
            }
            return map;
        }
    }

    [System.Serializable]
    internal class RegisterSimulatorBody
    {
#pragma warning disable 649
        public string slug;
        public string schema_id;
        public string rs_version;
#pragma warning restore 649
    }
    [System.Serializable]
    internal class RegisterBuildBody
    {
#pragma warning disable 649
        public string platform;
        public string filename;
#pragma warning restore 649
    }
}


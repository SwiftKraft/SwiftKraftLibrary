// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;

    [System.Serializable]
    internal class Release
    {
#pragma warning disable 649
        public string version;
        public string published_at;
        public Asset[] assets;
#pragma warning restore 649


        public static Release Get(string productID, string version)
        {
            string path = string.Format(Endpoints.releasePath, productID, version);
            PortalRequest req = new PortalRequest(path, "GET");
            req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            _ = req.SendWebRequest();
            while (!req.isDone)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Portal", $"Loading release assets for {version}...", req.downloadProgress))
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
                    Debug.Log(req.error);
                    return null;
            }
            return JsonUtility.FromJson<Release>(req.downloadHandler.text);
        }
    }
}

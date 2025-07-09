// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;

    [System.Serializable]
    internal class Asset
    {
#pragma warning disable 649
        public string name;
        public string created_at;
        public string download_url;
#pragma warning restore 649

        public bool Download(out byte[] data)
        {
            UnityWebRequest req = UnityWebRequest.Get(download_url);
            _ = req.SendWebRequest();

            while (!req.isDone)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Portal", $"Downloading {name}...", req.downloadProgress))
                {
                    EditorUtility.ClearProgressBar();
                    req.Abort();
                    data = null;
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
                    data = null;
                    return false;
            }

            data = req.downloadHandler.data;
            return true;
        }
    }
}

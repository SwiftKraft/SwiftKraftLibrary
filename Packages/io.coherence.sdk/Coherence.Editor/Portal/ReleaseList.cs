// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;

    [System.Serializable]
    internal class ReleaseList
    {

#pragma warning disable 649
        public Release[] releases;
        public int count;
#pragma warning restore 649

        private ReleaseList()
        {
        }

        private static bool Dev()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int idx = System.Array.IndexOf(args, "--coherence-dev-mode");
            if (idx != -1)
            {
                return true;
            }
            return false;
        }

        public static ReleaseList Get(string productID, int amount = 50)
        {
            string path = string.Format(Endpoints.releasesPath, productID, amount);
            if (Dev())
            {
                path = path + "&dev=true";
            }
            PortalRequest req = new PortalRequest(path, "GET");
            req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            _ = req.SendWebRequest();
            while (!req.isDone)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Portal", $"Loading releases for {productID}...", req.downloadProgress))
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
            return JsonUtility.FromJson<ReleaseList>(req.downloadHandler.text);
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Threading.Tasks;

    [System.Serializable]
    internal class RegionsList
    {

        public List<string> regions = new List<string>();

        private RegionsList()
        {
        }

        public static async Task<RegionsList> Fetch(string projectId)
        {
            using var req = new PortalRequest(Endpoints.regionsPath, "GET", true);
            req.downloadHandler = new DownloadHandlerBuffer();
            _ = req.SendWebRequest();
            while (!req.isDone)
            {
                await Task.Yield();
            }

            switch (req.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError($"Error fetching regions list: {req.error}");
                    return new RegionsList();
            }

            return JsonUtility.FromJson<RegionsList>(req.downloadHandler.text);
        }
    }
}

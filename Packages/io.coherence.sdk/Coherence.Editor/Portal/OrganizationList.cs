// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Net;
    using System.Threading.Tasks;

    [System.Serializable]
    internal class OrganizationList
    {
        public Organization[] orgs;
        public HttpStatusCode ResponseCode;

        private OrganizationList()
        {
        }

        public static async Task<OrganizationList> Fetch()
        {
            using var req = new PortalRequest(Endpoints.organizationsPath, "GET", true);
            req.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            _ = req.SendWebRequest();
            while (!req.isDone)
            {
                await Task.Yield();
            }

            OrganizationList orgList;
            switch (req.result)
            {
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.Log($"Error fetching organization list: {req.error}");
                    orgList = new OrganizationList();
                    orgList.ResponseCode = (HttpStatusCode)req.responseCode;
                    return orgList;
            }
            orgList = JsonUtility.FromJson<OrganizationList>(req.downloadHandler.text);
            orgList.ResponseCode = (HttpStatusCode)req.responseCode;
            return orgList;
        }
    }
}


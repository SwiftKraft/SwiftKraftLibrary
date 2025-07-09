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
    internal class ProjectInfo
    {
        private const string getProjectInfoUrlPath = "/project";
#pragma warning disable 649
        public string id;
        public string portal_token;
        public string runtime_key;
        public string name;
#pragma warning restore 649
    }
}

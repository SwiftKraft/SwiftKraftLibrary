// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Portal
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Networking;

    [System.Serializable]
    internal class Organization
    {
#pragma warning disable 649
        public string id;
        public string name;
        public string slug;
        public ProjectInfo[] projects;

#pragma warning restore 649
    }

}


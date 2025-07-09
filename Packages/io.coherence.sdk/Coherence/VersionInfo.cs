// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using Common;
    using UnityEngine;

    public class VersionInfo : ScriptableObject, IVersionInfo
    {
        public string Sdk => sdk;
        public string SdkRevisionHash { get => sdkRevisionHash; internal set => sdkRevisionHash = value; }
        public string SdkRevisionOrVersion => !string.IsNullOrEmpty(SdkRevisionHash) ? SdkRevisionHash : Sdk;
        public string Engine => engine;
        public string DocsSlug => docsSlug;

        [SerializeField] private string sdk;
        [SerializeField] private string sdkRevisionHash;
        [SerializeField] private string engine;
        [SerializeField] private string docsSlug;
    }
}


// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.UI
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public enum SamplePackageDependency
    {
        None = default,
        Ugui,
    }

    public enum SampleCategory
    {
        PackageSample,
        External,
    }

    [Serializable]
    [ExcludeFromPreset]
    public class SampleDialogAsset : ScriptableObject, IGridItem
    {
        public bool Enabled = true;
        public string SampleDisplayName;
        public string PrefabFileName;
        public SamplePackageDependency Dependency;
        public SampleCategory SampleCategory;
        public string CategoryDetail;

        public string Description;

        public Texture2D Preview;

        public Texture2D Thumbnail;

        public string ExternalUrl;

        public int Priority;

        public int Id => GetHashCode();
        public Texture2D Icon => Thumbnail;
        public string Label => SampleDisplayName;
    }
}

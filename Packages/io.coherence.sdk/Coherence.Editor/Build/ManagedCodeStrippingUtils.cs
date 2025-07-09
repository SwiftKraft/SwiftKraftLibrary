// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using Log;
    using UnityEditor;

    /// <summary>
    /// Utility methods related to managing managed code stripping done by the Unity linker.
    /// </summary>
    internal static class ManagedCodeStrippingUtils
    {
        private static readonly Lazy<Logger> logger = new(() => Log.GetLogger(typeof(ManagedCodeStrippingUtils)));
        private static Logger Logger => logger.Value;

        /// <summary>
        /// Should the <a href="https://docs.unity3d.com/Manual/managed-code-stripping-preserving.html#LinkXMLAnnotation">Link XML file</a>
        /// be copied from 'Packages/coherence/link.xml' to 'Assets/coherence/link.xml' to prevent Unity linker from stripping code from coherence's assemblies?
        /// </summary>
        public static bool PreventCodeStrippingUsingLinkXmlFile
#if UNITY_6000_0_OR_NEWER
        {
	        get
	        {
	            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
	            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
	            return PlayerSettings.GetManagedStrippingLevel(namedBuildTarget) != ManagedStrippingLevel.Disabled;
	        }
        }
#else
        => PlayerSettings.GetManagedStrippingLevel(EditorUserBuildSettings.selectedBuildTargetGroup) != ManagedStrippingLevel.Disabled;
#endif

        /// <summary>
        /// Copy the <a href="https://docs.unity3d.com/Manual/managed-code-stripping-preserving.html#LinkXMLAnnotation">Link XML file</a>
        /// from 'Packages/coherence/link.xml' to 'Assets/coherence/link.xml' to prevent Unity linker from stripping code from coherence's assemblies.
        /// </summary>
        public static bool CopyLinkXmlFileUnderAssetsFolder()
        {
            var sourcePath = Paths.linkXmlPackagesPath;
            var destinationPath = Paths.linkXmlAssetsPath;

            if (!File.Exists(sourcePath))
            {
                Logger.Error(Error.LinkXmlNotFound, $"Can't find link.xml at '{sourcePath}'. Won't be able to prevent managed code stripping of coherence's assemblies.");
                return false;
            }

            try
            {
                if (AssetUtils.CopyFile(sourcePath, destinationPath, ImportAssetOptions.ForceSynchronousImport))
                {
                    return true;
                }

                Logger.Error(Error.CopyLinkXmlFailed, $"Failed to copy link.xml file to {destinationPath}.");
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(Error.CopyLinkXmlFailed, $"Failed to copy link.xml file to {destinationPath}.", ("Exception", e));
                return false;
            }
        }

        public static bool DeleteLinkXmlFileUnderAssetsFolder()
        {
            var success = AssetUtils.DeleteFile(Paths.linkXmlAssetsPath);
            AssetUtils.DeleteFolderIfEmpty(Path.GetDirectoryName(Paths.linkXmlAssetsPath));
            return success;
        }
    }
}

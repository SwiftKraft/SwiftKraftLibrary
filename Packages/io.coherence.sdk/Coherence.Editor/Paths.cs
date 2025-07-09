// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using Log;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Class listing various paths of interest.
    /// <para>
    /// All paths are relative to the project folder (e.g. 'Assets/StreamingAssets'), unless the variable name
    /// specifically contains the word 'Absolute', in which case it contains a fully qualified path
    /// (e.g. 'c:/Unity Projects/My Project/Assets/StreamingAssets').
    /// </para>
    /// </summary>
    public static class Paths
    {
        private const string assetsPath = "Assets";
        private const string packagesPath = "Packages";
        private const string libraryPath = "Library";

        public const string packageId = "io.coherence.sdk";
        public const string packageRootPath = packagesPath + "/" + packageId;
        public const string docFxPath = packageRootPath + "/docfx~";
        public const string docFxConfigPath = docFxPath + "/docfx.json";
        public const string docFxSitePath = docFxPath + "/_site";
        public const string docFxSiteZipPath = docFxPath + "/_site.zip";
        public const string docFxDllsPath = docFxPath + "/dlls";
        public const string versionInfoPath = packageRootPath + "/Coherence/VersionInfo.asset";
        public const string packageManifestPath = packageRootPath + "/package.json";
        public const string iconsPath = packageRootPath + "/Coherence.Editor/Icons";
        public const string uiAssetsPath = packageRootPath + "/Coherence.Editor/UIAssets";
        public const string welcomeWindowPath = packageRootPath + "/Coherence.Editor/WelcomeWindow";
        public const string sampleDialogPickerPath = packageRootPath + "/Coherence.Editor/UI/SampleDialogPicker";
        public const string toolsPath = packageRootPath + "/Runtime~";
        public const string toolkitSchemaPath = packageRootPath + "/Coherence.Toolkit/Toolkit.schema";
        public const string rsSchemaPath = packageRootPath + "/Coherence.Toolkit/RS.schema";
        public const string dummiesPath = packageRootPath + "/Coherence.Editor/Build/Dummies~";
        public const string providerTemplatePath = packageRootPath + "/Coherence/CustomProvider.cs.template";
        public const string instantiatorTemplatePath = packageRootPath + "/Coherence/CustomInstantiator.cs.template";
        public const string scriptAssembliesPath = libraryPath + "/ScriptAssemblies";

        public const string xmlDocsRelativePath = "Temp/Docs";

        public const string unityMetaFileExtension = ".meta";
        public const string directoryBuildTargetsFile = "Directory.Build.targets";


        /// <summary>
        /// Path to the <a href="https://docs.unity3d.com/Manual/managed-code-stripping-preserving.html#LinkXMLAnnotation">Link XML file</a>
        /// located under the packages folder.
        /// <para>
        /// Assemblies can be listed in this file to prevent Unity linker from stripping code from them when creating IL2CPP builds
        /// with managed code stripping enabled.
        /// </para>
        /// <para>
        /// This file needs to be copied under the 'Assets' folder to have it take effect.
        /// </para>
        /// </summary>
        public const string linkXmlPackagesPath = packageRootPath + "/link.xml";

        /// <summary>
        /// Path under the 'Assets' folder where the
        /// <a href="https://docs.unity3d.com/Manual/managed-code-stripping-preserving.html#LinkXMLAnnotation">Link XML file</a>
        /// located at the <see cref="linkXmlPackagesPath"/> path should be copied to prevent Unity linker from stripping code
        /// from coherence's assemblies.
        /// </summary>
        public const string linkXmlAssetsPath = projectAssetsPath + "/Temp/link.xml";

        /// <summary>
        /// Path to the library folder that contains coherence's 'Editor.log' and 'Editor.previous.log' files.
        /// </summary>
        public const string CoherenceLogFilesPath = libraryRootPath;

        /// <summary>
        /// Path to coherence's 'Editor.log' file.
        /// </summary>
        public const string CurrentCoherenceLogFilePath = CoherenceLogFilesPath + "/Editor.log";

        /// <summary>
        /// Path to coherence's 'Editor.previous.log' file.
        /// </summary>
        public const string PreviousCoherenceLogFilePath = CoherenceLogFilesPath + "/Editor.previous.log";

        private const string osxToolsPathSubDirectory = "darwin";
        private const string windowsToolsPathSubDirectory = "windows";
        private const string linuxToolsPathSubDirectory = "linux";

#if UNITY_EDITOR_OSX
        private const string osxNativeToolsPath = toolsPath + "/" + osxToolsPathSubDirectory;
#elif UNITY_EDITOR_WIN
        private const string windowsNativeToolsPath = toolsPath + "/" + windowsToolsPathSubDirectory;
#else
        private const string linuxNativeToolsPath = toolsPath + "/" + linuxToolsPathSubDirectory;
#endif

        public static readonly string nativeToolsPath =
#if UNITY_EDITOR_OSX
            osxNativeToolsPath;
#elif UNITY_EDITOR_WIN
            windowsNativeToolsPath;
#else
            RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? linuxNativeToolsPath + "-arm64"
                : linuxNativeToolsPath;
#endif

#if UNITY_EDITOR_WIN
        public const string nativeToolExtension = ".exe";
#else
        public const string nativeToolExtension = "";
#endif

        public static readonly string directoryBuildTargetsPath;
        public static readonly string xmlDocsAbsolutePath;

        public static readonly string projectAbsolutePath = Path.GetDirectoryName(Application.dataPath).Replace('\\', '/');

        /// <summary>
        /// Absolute path to the 'Assets' folder.
        /// </summary>
        public static readonly string assetsAbsolutePath = Application.dataPath.Replace('\\', '/');
        public static readonly string packagesAbsolutePath = projectAbsolutePath + "/Packages";

        public static readonly string streamingAssetsPath = PathUtils.GetRelativePath(Application.streamingAssetsPath);

        /// <summary>
        /// Path to the 'Editor' folder under local application data that contains the 'Editor.log' and 'Editor-prev.log' files.
        /// </summary>
        public static readonly string EditorLogFilesAbsolutePath;

        /// <summary>
        /// Path to Unity's 'Editor.log' file under local application data.
        /// </summary>
        public static readonly string CurrentEditorLogFileAbsolutePath;

        /// <summary>
        /// Path to Unity's 'Editor-prev.log' file under local application data.
        /// </summary>
        public static readonly string PreviousEditorLogFileAbsolutePath;

        static Paths()
        {
            directoryBuildTargetsPath = projectAbsolutePath + "/" + directoryBuildTargetsFile;
            xmlDocsAbsolutePath = projectAbsolutePath + "/" + xmlDocsRelativePath;

            EditorLogFilesAbsolutePath =
#if UNITY_EDITOR_LINUX
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.config/unity3d";
#elif UNITY_EDITOR_OSX
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Library/Logs/Unity";
#elif UNITY_EDITOR_WIN
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/Unity/Editor";
#else
                "";
#endif

            CurrentEditorLogFileAbsolutePath = EditorLogFilesAbsolutePath + "/Editor.log";


            PreviousEditorLogFileAbsolutePath = EditorLogFilesAbsolutePath + "/Editor-prev.log";
        }

        public static string GetToolsPath(BuildTarget platform)
        {
            if (ProjectSettings.instance.UseCustomTools)
            {
                return ProjectSettings.instance.CustomToolsPath;
            }

            var subDirectory = platform switch
            {
                BuildTarget.StandaloneOSX => osxToolsPathSubDirectory,
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => windowsToolsPathSubDirectory,
                BuildTarget.StandaloneLinux64 => linuxToolsPathSubDirectory,
                _ => throw new ArgumentException($"Can't get tools path for platform: {platform}"),
            };

            return ProjectSettings.instance.UseCustomTools
                       ? ProjectSettings.instance.CustomToolsPath
                       : Path.Combine(toolsPath, subDirectory);
        }

        public static string ReplicationServerPath => ProjectSettings.instance.UseCustomTools
            ? Path.Combine(ProjectSettings.instance.CustomToolsPath, replicationServerName)
            : defaultReplicationServerPath;

        private const string replicationServerNameWithoutExtensions = "replication-server";
        public const string replicationServerName = replicationServerNameWithoutExtensions + nativeToolExtension;

        public static string defaultReplicationServerPath = nativeToolsPath + "/" + replicationServerName;

        public const string projectSettingsPath = "ProjectSettings/CoherenceSettings.asset";
        public const string simulatorProjectSlugsPath = assetsPath + "/coherence/slugs.json";
        public const string projectSettingsWindowPath = "Project/coherence";

        public const string projectAssetsPath = assetsPath + "/" + "coherence";
        public const string defaultSchemaBakePath = projectAssetsPath + "/baked";
        public const string simulatorBuildOptionsPath = projectAssetsPath + "/SimulatorBuildOptions.asset";
        public const string coherenceSyncConfigPath = projectAssetsPath + "/CoherenceSyncConfigs";

        public const string libraryRootPath = libraryPath + "/" + "coherence";
        public const string gatherSchemaPath = projectAssetsPath + "/Gathered.schema";
        public const string combinedSchemaPath = libraryRootPath + "/combined.schema";
        public static readonly string streamingAssetsCombinedSchemaPath = streamingAssetsPath + "/combined.schema";
        public const string assetBackupPath = libraryRootPath + "/AssetsBackup";

        public const string defaultPersistentStorageFolderPath = libraryRootPath + "/PersistentData";
        public const string defaultPersistentStorageFileName = "world";
        public const string persistentStorageFileExtension = "json";

        public const string defaultPersistentStoragePath = defaultPersistentStorageFolderPath + "/" +
                                                           defaultPersistentStorageFileName + "." +
                                                           persistentStorageFileExtension;

        public const string simulatorZipFile = "simulator.zip";
        public const string gameZipFile = "game.zip";

        public const string uiRootPath = packageRootPath + "/Coherence.UI";
        public const string uiPrefabsPath = uiRootPath + "/Prefabs";
        public static string[] AllSchemas =
        {
            toolkitSchemaPath,
            gatherSchemaPath,
        };
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    internal abstract class BuildUploader
    {
        private class BuildUploadEventProperties : Analytics.BaseProperties
        {
            public string platform;
        }

        protected virtual string DialogTitle => "Compress and upload?";
        protected virtual string Message => "Contents in the following path will be compressed and uploaded. Are you sure?\n\n";
        protected virtual string OkButton => "Compress and upload";
        protected virtual string CancelButton => "Cancel";

        internal abstract bool Upload(AvailablePlatforms platform, string buildPath);
        
        internal bool AllowUpload(string buildPath)
        {
            return EditorUtility.DisplayDialog(DialogTitle, $"{Message}{buildPath}", OkButton, CancelButton);
        }

        protected string GetZipTempPath()
        {
            return Path.Combine(Application.temporaryCachePath, Paths.gameZipFile);
        }

        protected void OnUploadStart(string platform)
        {
            Analytics.Capture(new Analytics.Event<BuildUploadEventProperties>(
                Analytics.Events.UploadStart,
                new BuildUploadEventProperties {
                    platform = platform
                }
            ));
        }

        protected void OnUploadEnd(string platform)
        {
            Analytics.Capture(new Analytics.Event<BuildUploadEventProperties>(
                Analytics.Events.UploadEnd,
                new BuildUploadEventProperties {
                    platform = platform
                }
            ));
        }
    }
}

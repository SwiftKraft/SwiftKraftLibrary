// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using UnityEngine.UIElements;

    [Serializable, HubModule(Priority = 100)]
    public class LearnModule : HubModule
    {
        private struct UpdateInfo
        {
            public string Version;
            public string OldVersion;
            public HashSet<string> MigrationMessages;
        }

        public override string ModuleName => "Learn";

        private UpdateInfo updateInfo;
        private bool setUpdateInfo;
        private WelcomeWindow welcomeWindow;

        internal void SetUpdateInfo(string oldVersion, string version, HashSet<string> migrationMessages)
        {
            if (welcomeWindow == null)
            {
                setUpdateInfo = true;
                updateInfo = new UpdateInfo()
                {
                    Version = version,
                    OldVersion = oldVersion,
                    MigrationMessages = migrationMessages
                };
            }
            else
            {
                welcomeWindow.SetUpdateInfo(oldVersion, version, migrationMessages);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DestroyImmediate(welcomeWindow);
        }

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();
            GetOrCreateWindow().Load(root);
            return root;
        }

        private WelcomeWindow GetOrCreateWindow()
        {
            if (!welcomeWindow)
            {
                welcomeWindow = CreateInstance<WelcomeWindow>();
                if (setUpdateInfo)
                {
                    welcomeWindow.SetUpdateInfo(updateInfo.OldVersion, updateInfo.Version,
                        updateInfo.MigrationMessages);
                    setUpdateInfo = false;
                }
            }

            return welcomeWindow;
        }
    }
}

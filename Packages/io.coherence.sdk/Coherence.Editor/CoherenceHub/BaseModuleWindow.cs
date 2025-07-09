// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    public class BaseModuleWindow : EditorWindow
    {
        protected HubModule module;
        private CoherenceHeader headerDrawer;

        protected virtual void Awake()
        {
            // see ModuleWindow
        }

        protected virtual void OnEnable()
        {
            InitHeader();
            titleContent = module.TitleContent;
            headerDrawer ??= new CoherenceHeader(this);
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void OnDestroy()
        {
            HubModuleManager.instance.ReleaseModule(module);
        }

        protected virtual void OnGUI()
        {
            InitHeader();
            headerDrawer.OnGUI();

            OnWindowGUI();
        }

        protected virtual void OnWindowGUI()
        {
            EditorGUILayout.HelpBox("Undocked windows have been deprecated. Please use the Hub directly.", MessageType.Warning);

            if (!module)
            {
                return;
            }

            if (GUILayout.Button("Open in Hub", ContentUtils.GUIStyles.bigButton))
            {
                var hub = CoherenceHub.Open();
                if (!hub.FocusModule(module))
                {
                    hub.ShowNotification(new GUIContent($"'{module.TitleContent.text}' not found in Hub"));
                }
                Close();
            }
        }

        private void InitHeader()
        {
            if (headerDrawer == null)
            {
                headerDrawer = new CoherenceHeader(this);
            }
        }
    }
}

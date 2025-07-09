// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;
    using Logger = Log.Logger;

    public interface IModuleWindow : IAnyHubModule
    {
        HubModule GetData();
    }

    public interface IAnyHubModule
    {
        public Logger Logger { get; set; }
    }

    public abstract class ModuleWindow<TWindow, TData> : BaseModuleWindow, IModuleWindow where TWindow : EditorWindow, IModuleWindow where TData : HubModule
    {
        public Logger Logger { get; set; }

        public string WindowName => typeof(TData).Name;

        public static void OpenWindow(bool shouldOpen)
        {
            if (shouldOpen)
            {
                _ = GetWindow<TWindow>();
            }
            else
            {
                GetWindow<TWindow>().Close();
            }
        }

        public HubModule GetData() => HubModuleManager.instance.GetActiveModule(this);

        protected override void Awake()
        {
            base.Awake();
            HubModuleManager.instance.AssignModule(this, out TData m, HubModuleManager.AssignStrategy.ForceReassign);
            module = m;
        }
    }
}

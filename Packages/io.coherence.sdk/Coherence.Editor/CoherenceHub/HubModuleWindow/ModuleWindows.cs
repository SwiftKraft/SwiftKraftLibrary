// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEditor;

    public class LinksModuleWindow : ModuleWindow<LinksModuleWindow, LinksModule>
    {
    }

    public class SceneModuleWindow : ModuleWindow<SceneModuleWindow, QuickStartModule>
    {
    }

    public class SimulatorsModuleWindow : ModuleWindow<SimulatorsModuleWindow, SimulatorsModule>
    {
    }

    public class GameObjectModuleWindow : ModuleWindow<GameObjectModuleWindow, GameObjectModule>
    {
    }

    public class LocalServerModuleWindow : ModuleWindow<LocalServerModuleWindow, ReplicationServerModule>
    {
    }

    public class SchemaAndBakeModuleWindow : ModuleWindow<SchemaAndBakeModuleWindow, NetworkedPrefabsModule>
    {
    }

    public class OnlineModuleWindow : ModuleWindow<OnlineModuleWindow, CloudModule>
    {
    }

    public class CoherenceSyncObjectsWindow : ModuleWindow<CoherenceSyncObjectsWindow, CoherenceSyncObjectsModule>
    {
    }

    public class MultiRoomSimulatorsWizardModuleWindow : ModuleWindow<MultiRoomSimulatorsWizardModuleWindow, MultiRoomSimulatorsWizardModule>
    {
        protected override void Awake()
        {
            module = CreateInstance<MultiRoomSimulatorsWizardModule>();
        }

        protected override void OnGUI()
        {
            CoherenceHeader.DrawSlimHeader(null);
            EditorGUILayout.HelpBox("Multi-Room Simulators are deprecated and will be completely removed in the future.", MessageType.Warning);
            module.OnGUI();
        }

        protected override void OnDestroy()
        {
            DestroyImmediate(module);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCCore : MonoBehaviour
    {
        public readonly Dictionary<string, NPCModuleBase> Modules = new();

        public bool TryGetModule<T>(string id, out T module) where T : NPCModuleBase
        {
            module = GetModule<T>(id);
            return module != null;
        }

        public T GetModule<T>(string id) where T : NPCModuleBase => Modules.ContainsKey(id) && Modules[id] is T t ? t : null;

        public bool AddModule(string id, NPCModuleBase reference)
        {
            if (Modules.ContainsKey(id))
                return false;

            Modules.Add(id, reference);
            return true;
        }

        public bool RemoveModule(string id) => Modules.Remove(id);

        public bool RemoveModule(NPCModuleBase reference) => RemoveModule(reference.ID);
    }

    [RequireComponent(typeof(NPCCore))]
    public abstract class NPCModuleBase : MonoBehaviour
    {
        public virtual string ID => "Module";

        public NPCCore Parent { get; private set; }

        protected virtual void Awake()
        {
            Parent = GetComponent<NPCCore>();
            Parent.AddModule(ID, this);
        }
    }
}

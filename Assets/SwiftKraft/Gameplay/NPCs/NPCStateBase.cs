using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public abstract class NPCStateBase : ScriptableObject
    {
        public NPCCore Core { get; private set; }

        public virtual void Init(NPCCore core) => Core = core;

        public abstract void Begin();
        public abstract void Tick();
        public abstract void End();
    }
}

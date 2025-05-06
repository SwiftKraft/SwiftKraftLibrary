using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameObject("NPCManager").AddComponent<NPCManager>();

                return _instance;
            }
            private set => _instance = value;
        }

        public readonly List<NPCCore> NPCs = new();
        static NPCManager _instance;

        private void Update()
        {
            for (int i = 0; i < NPCs.Count; i++)
                if (NPCs[i].enabled)
                    NPCs[i].Frame();
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < NPCs.Count; i++)
                if (NPCs[i].enabled)
                    NPCs[i].Tick();
        }
    }
}

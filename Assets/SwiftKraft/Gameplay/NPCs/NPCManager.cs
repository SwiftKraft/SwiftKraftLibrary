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
                    new GameObject("NPCManager").AddComponent<NPCManager>();

                return _instance;
            }
            private set => _instance = value;
        }
        static NPCManager _instance;

        public readonly List<NPCCore> NPCs = new();

        private void Awake()
        {
            if (_instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

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

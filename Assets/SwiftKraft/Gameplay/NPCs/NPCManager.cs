using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwiftKraft.Gameplay.NPCs
{
    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance { get; private set; }

        public readonly List<NPCCore> NPCs = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void FixedUpdate()
        {
            if (NPCs.Count == 0) return;

            for (int i = 0; i < NPCs.Count; i++)
                if (NPCs[i].enabled)
                    NPCs[i].Tick();
        }
    }
}

using SwiftKraft.UI.Menus;
using SwiftKraft.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Gameplay.NPCs.Demo
{
    public class SpawnMenu : MenuBase
    {
        public Prefab[] Prefabs;
        [Header("References")]
        public Transform ContentGroup;
        public GameObject SpawnButtonPrefab;

        BooleanLock.Lock cursorLock;

        private void Awake() => cursorLock = CursorManager.Unlocked.AddLock();
        private void Start()
        {
            for (int i = 0; i < Prefabs.Length; i++)
            {
                SpawnButton button = Instantiate(SpawnButtonPrefab, ContentGroup).GetComponent<SpawnButton>();
                
                button.Spawnable = Prefabs[i].Spawnable;
                button.GetComponentInChildren<TMP_Text>().SetText(button.Spawnable.name);

                if (Prefabs[i].Icon != null)
                    button.Icon.sprite = Prefabs[i].Icon;
            }
        }
        private void OnDestroy() => cursorLock.Dispose();

        protected override void ActiveChanged(bool active)
        {
            base.ActiveChanged(active);
            cursorLock.Active = !CursorManager.DefaultUnlocked && active;
        }

        [Serializable]
        public struct Prefab
        {
            public GameObject Spawnable;
            public Sprite Icon;
        }
    }
}

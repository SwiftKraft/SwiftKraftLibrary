using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class FPSInventoryItem : MonoBehaviour
    {
        public FPSInventoryMenu Parent { get; private set; }

        public GameObject ItemActionsList;
        public GameObject ItemActionUI;
        public Transform ActionZone;

        private void Awake() => Parent = GetComponentInParent<FPSInventoryMenu>();

        public void SpawnActions()
        {

        }

        public void ToggleActionsList() => SetActionsList(!ItemActionsList.activeSelf);

        public void SetActionsList(bool active) => ItemActionsList.SetActive(active);
    }
}

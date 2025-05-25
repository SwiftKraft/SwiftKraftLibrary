using SwiftKraft.UI.Menus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class FPSInventoryMenu : MenuBase
    {
        public SimpleFPSInventory Inventory { get; private set; }

        private void Awake() => MenuOpen.UpdateOpen += MenuOpen_UpdateOpen;

        private void OnDestroy() => MenuOpen.UpdateOpen -= MenuOpen_UpdateOpen;

        private void MenuOpen_UpdateOpen(bool obj)
        {
            if (obj)
                Active = false;
        }

        private void OnEnable()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }

        private void OnDisable()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}

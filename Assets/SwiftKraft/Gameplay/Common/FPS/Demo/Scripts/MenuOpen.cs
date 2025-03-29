using SwiftKraft.Gameplay.Motors;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class MenuOpen : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        public static event Action<bool> UpdateOpen;

        public GameObject MenuObject;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetMenu(!MenuObject.activeSelf);
            }
        }

        public void SetMenu(bool active)
        {
            MenuObject.SetActive(active);
            Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = active;
            IsOpen = active;
            UpdateOpen?.Invoke(IsOpen);
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}

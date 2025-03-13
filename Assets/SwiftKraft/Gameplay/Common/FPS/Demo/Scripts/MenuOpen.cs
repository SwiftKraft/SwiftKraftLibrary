using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class MenuOpen : MonoBehaviour
    {
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
            FindObjectOfType<FPSCharacterControllerMotor>().Enabled = !active;
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}

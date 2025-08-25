using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs.Demo
{
    public class Menu : MonoBehaviour
    {
        public SpawnMenu SpawnMenu;

        private void Start() => SpawnMenu.Active = false;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                SpawnMenu.Active = !SpawnMenu.Active;
        }
    }
}

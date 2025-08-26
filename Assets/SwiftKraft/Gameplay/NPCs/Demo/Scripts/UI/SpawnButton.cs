using UnityEngine;
using UnityEngine.UI;

namespace SwiftKraft.Gameplay.NPCs.Demo
{
    public class SpawnButton : MonoBehaviour
    {
        public SpawnMenu Menu { get; private set; }

        public GameObject Spawnable;
        public Image Icon;

        private void Awake() => Menu = GetComponentInParent<SpawnMenu>();

        public void SetSpawnable()
        {
            Spawner.Prefab = Spawnable;
            Menu.Active = false;
        }
    }
}

using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    public class PlayerSpawn : MonoBehaviour
    {
        public static PlayerSpawn Instance { get; private set; }

        public GameObject PlayerPrefab;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void Spawn() => Instantiate(PlayerPrefab, transform.position, transform.rotation);
    }
}

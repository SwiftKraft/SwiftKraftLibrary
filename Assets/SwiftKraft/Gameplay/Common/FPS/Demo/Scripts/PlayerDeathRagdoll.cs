using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.Demo
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerDeathRagdoll : MonoBehaviour
    {
        private void OnEnable() => GetComponent<Rigidbody>().AddTorque(Vector3.right * 20f, ForceMode.Impulse);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5) && PlayerSpawn.Instance != null)
            {
                Destroy(transform.parent.gameObject);
                PlayerSpawn.Instance.Spawn();
            }
        }
    }
}

using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs.Demo
{
    public class Spawner : MonoBehaviour
    {
        public static Spawner Instance { get; private set; }

        public static GameObject Prefab { get; set; }

        public LayerMask RayLayers;

        public Material IndicatorMaterial;

        public GameObject IndicatorCube { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            IndicatorCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            IndicatorCube.GetComponent<MeshRenderer>().material = IndicatorMaterial;
            Destroy(IndicatorCube.GetComponent<Collider>());
        }

        private void Update()
        {
            if (InputBlocker.Blocked)
                return;

            if (Input.GetKeyDown(KeyCode.Return))
                Time.timeScale = Time.timeScale == 1f ? 0f : 1f;
            if (Input.GetKeyDown(KeyCode.R))
                Time.timeScale = Time.timeScale == 1f ? 0.5f : 1f;

            if (Prefab == null)
            {
                IndicatorCube.SetActive(false);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Q))
                Rotation *= Quaternion.Euler(0f, 15f, 0f);
            else if (Input.GetKeyDown(KeyCode.E))
                Rotation *= Quaternion.Euler(0f, -15f, 0f);

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Mathf.Infinity, RayLayers, QueryTriggerInteraction.Ignore))
            {
                IndicatorCube.SetActive(true);
                IndicatorCube.transform.SetPositionAndRotation(hit.point + Vector3.up * 0.5f, Rotation);
                if (Input.GetKeyDown(KeyCode.Mouse0))
                    Instantiate(Prefab, hit.point, Rotation);
            }
            else
                IndicatorCube.SetActive(false);

            if (Input.GetKeyDown(KeyCode.Mouse1))
                Prefab = null;
        }
    }
}

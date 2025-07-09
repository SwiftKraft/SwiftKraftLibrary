namespace Coherence.Samples
{
    using System.Collections.Generic;
    using System.Collections;
    using Toolkit;
    using UnityEngine;

    public class PhysicsSimulation : MonoBehaviour
    {
        public CoherenceSync spherePrefab;
        public GameObject loadingScreen;
        public float speed = 1f;

        private int gridSize = 23;
        private float spacing = 1f;
        private int maxObjects = 300;

        private float minRotation = -20f;
        private float maxRotation = 20f;

        private float currentRotation;
        private bool invertRotation;

        private float interpTime;

        private bool startRotation;
        private List<CoherenceSync> spawnedObjects;

        private void Awake()
        {
            CoherenceBridgeStore.TryGetBridge(gameObject.scene, out CoherenceBridge coherenceBridge);
            coherenceBridge.onConnected.AddListener(_ =>
            {
                loadingScreen.SetActive(true);
            });
            coherenceBridge.onLiveQuerySynced.AddListener(_ =>
            {
                loadingScreen.SetActive(false);

                if (!GetComponent<CoherenceSync>().HasStateAuthority)
                {
                    return;
                }

                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
                SpawnSpheres();
                StartCoroutine(StartRotating());
            });
            coherenceBridge.onDisconnected.AddListener((_, _) =>
            {
                if (spawnedObjects == null)
                {
                    return;
                }

                foreach (var obj in spawnedObjects)
                {
                    Destroy(obj.gameObject);
                }
                spawnedObjects.Clear();
            });
        }

        private IEnumerator StartRotating()
        {
            yield return new WaitForSeconds(1f);
            startRotation = true;
        }

        private void Update()
        {
            if (!startRotation)
            {
                return;
            }

            transform.Rotate(new Vector3(currentRotation, currentRotation, currentRotation) * Time.deltaTime);

            if (!invertRotation)
            {
                currentRotation = Mathf.Lerp(minRotation, maxRotation, interpTime);
                interpTime += speed * Time.deltaTime;

                if (interpTime >= 1f)
                {
                    interpTime = 0f;
                    invertRotation = true;
                }
            }
            else
            {
                currentRotation = Mathf.Lerp(maxRotation, minRotation, interpTime);
                interpTime += speed * Time.deltaTime;

                if (interpTime >= 1f)
                {
                    interpTime = 0f;
                    invertRotation = false;
                }
            }
        }

        private void SpawnSpheres()
        {
            int objectsSpawned = 0;
            spawnedObjects = new List<CoherenceSync>(maxObjects);
            for (int x = 0; x < gridSize && objectsSpawned < maxObjects; x++)
            {
                for (int z = 0; z < gridSize && objectsSpawned < maxObjects; z++)
                {
                    float xPos = -11f + (x * spacing);
                    float zPos = -11f + (z * spacing);
                    Vector3 spawnPosition = new Vector3(xPos, 2.5f, zPos);

                    CoherenceSync newObject = Instantiate(spherePrefab, spawnPosition, Quaternion.identity);
                    spawnedObjects.Add(newObject);
                    objectsSpawned++;
                }
            }
        }
    }
}

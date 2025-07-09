namespace Coherence.Samples.PlayerSpawner
{
    using UnityEngine;
    using Toolkit;

    public class Client : MonoBehaviour
    {
        private CoherenceSync localPlayerInstance;
        
        /// <summary> This Network Command is sent by the SpawnManager script from the Client that has authority over it. </summary>
        [Command]
        public void SpawnPlayer(Vector3 spawnPosition)
        {
            SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();
            localPlayerInstance = spawnManager.Spawn(spawnPosition);
        }

        private void OnDisable()
        {
            // We destroy the local player instance, so in case this Client reconnects
            // they don't bring a duplicate to the simulation.
            if (localPlayerInstance != null)
            {
                Destroy(localPlayerInstance.gameObject);
            }
        }
    }   
}

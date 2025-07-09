namespace Coherence.Samples.PlayerSpawner
{
    using UnityEngine;
    using Connection;
    using Toolkit;
    using System.Collections.Generic;

    public class SpawnManager : MonoBehaviour
    {
        [SerializeField]
        private List<Transform> spawnPoints;

        [SerializeField]
        private CoherenceSync playerPrefab;

        private CoherenceBridge coherenceBridge;
        private CoherenceSync coherenceSync;

        private Queue<Transform> unusedSpawnPoints;
        private Dictionary<ClientID, Transform> assignedClients;

        private void Awake()
        {
            _ = CoherenceBridgeStore.TryGetBridge(gameObject.scene, out coherenceBridge);
            coherenceSync = GetComponent<CoherenceSync>();
        }

        private void OnEnable()
        {
            coherenceBridge.onLiveQuerySynced.AddListener(OnLiveQuery);
            coherenceBridge.onDisconnected.AddListener(OnDisconnected);
        }

        private void OnDisable()
        {
            coherenceBridge.onLiveQuerySynced.RemoveListener(OnLiveQuery);
            coherenceBridge.onDisconnected.RemoveListener(OnDisconnected);
        }

        private void OnLiveQuery(CoherenceBridge _)
        {
            if (!coherenceSync.HasStateAuthority)
            {
                return;
            }

            unusedSpawnPoints = new Queue<Transform>(spawnPoints);
            assignedClients = new();

            AssignSpawnPointToClient(coherenceBridge.ClientConnections.GetMine()); // For this Client

            coherenceBridge.ClientConnections.OnCreated += AssignSpawnPointToClient; // For future Clients
            coherenceBridge.ClientConnections.OnDestroyed += RecycleClientSpawnPoint;

            AssignSpawnPointToOtherClients();
        }

        private void AssignSpawnPointToOtherClients()
        {
            foreach (CoherenceClientConnection clientConnection in coherenceBridge.ClientConnections.GetOther())
            {
                AssignSpawnPointToClient(clientConnection);
            }
        }

        private void AssignSpawnPointToClient(CoherenceClientConnection clientConnection)
        {
            ClientID clientID = clientConnection.ClientId;

            bool hasIndexAssigned = assignedClients.ContainsKey(clientID);
            if (hasIndexAssigned)
            {
                return;
            }

            if (unusedSpawnPoints.TryDequeue(out var spawnPoint))
            {
                _ = clientConnection.SendClientMessage<Client>(nameof(Client.SpawnPlayer), MessageTarget.AuthorityOnly, spawnPoint.position);

                assignedClients.Add(clientID, spawnPoint);
            }
            else
            {
                Debug.LogWarning($"No more spawn points available! Not spawning");
            }
        }

        private void RecycleClientSpawnPoint(CoherenceClientConnection clientConnection)
        {
            ClientID clientID = clientConnection.ClientId;

            if (assignedClients.TryGetValue(clientID, out var spawnPoint))
            {
                unusedSpawnPoints.Enqueue(spawnPoint);

                _ = assignedClients.Remove(clientID);
            }
        }

        public CoherenceSync Spawn(Vector3 worldPosition)
        {
            return Instantiate(playerPrefab, worldPosition, Quaternion.identity);
        }

        private void OnDisconnected(CoherenceBridge _, ConnectionCloseReason __)
        {
            assignedClients = null;
            unusedSpawnPoints = null;
            coherenceBridge.ClientConnections.OnCreated -= AssignSpawnPointToClient;
            coherenceBridge.ClientConnections.OnDestroyed -= RecycleClientSpawnPoint;
        }
    }
}

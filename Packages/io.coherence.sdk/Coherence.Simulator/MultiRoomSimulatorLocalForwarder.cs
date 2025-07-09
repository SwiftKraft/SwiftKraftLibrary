// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Simulator
{
    using System;
    using UnityEngine.Networking;
    using Toolkit;
    using Connection;
    using Log;
    using Logger = Log.Logger;
    using System.Collections;
    using System.Collections.Generic;

#if COHERENCE_ENABLE_MULTIROOM_SIMULATOR
    [UnityEngine.AddComponentMenu("coherence/Multi-Room Simulators/Coherence Multi-Room Simulator Local Forwarder")]
#endif
    [NonBindable]
    [Obsolete("ConnectionEventHandler is being deprecated, and will be completely removed in future releases.")]
    [Deprecated("17/03/2024", 1, 6, 0)]
    public sealed class MultiRoomSimulatorLocalForwarder : CoherenceBehaviour
    {
        private readonly Logger logger = Log.GetLogger<MultiRoomSimulatorLocalForwarder>();

        private ICoherenceBridge bridge;

        private List<EndpointData> endpointDatas = new List<EndpointData>();

        public ICoherenceBridge Bridge
        {
            get => bridge;
            private set
            {
                if (bridge != null)
                {
                    bridge.Client.OnConnectedEndpoint -= OnConnectedEndpoint;
                }
                bridge = value;
                if (bridge != null)
                {
                    bridge.Client.OnConnectedEndpoint += OnConnectedEndpoint;
                }
            }
        }

        private void OnEnable()
        {
            if (CoherenceBridgeStore.TryGetBridge(gameObject.scene, out var bridge))
            {
                Bridge = bridge;
            }
        }

        private void OnDisable()
        {
            Bridge = null;
        }

        private void OnConnectedEndpoint(EndpointData endpointData)
        {
            if (!RuntimeSettings.Instance || !RuntimeSettings.Instance.LocalDevelopmentMode || SimulatorUtility.IsCloudSimulator)
            {
                return;
            }

            endpointDatas.Add(endpointData);
        }

        private void Update()
        {
            if (!RuntimeSettings.Instance || !RuntimeSettings.Instance.LocalDevelopmentMode || SimulatorUtility.IsCloudSimulator)
            {
                return;
            }

            if (endpointDatas.Count > 0)
            {
                foreach (var endpointData in endpointDatas)
                {
                    _ = StartCoroutine(SendJoinRequest(endpointData));
                }

                endpointDatas.Clear();
            }
        }

        private IEnumerator SendJoinRequest(EndpointData endpointData)
        {
            var joinRoomRequest = JoinRoomRequest.FromEndpointData(endpointData);
            var json = Utils.CoherenceJson.SerializeObject(joinRoomRequest);

            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

            var rs = RuntimeSettings.Instance;
            var url = $"{rs.LocalHttpServerHost}:{rs.LocalHttpServerPort}/rooms";
            using (var req = UnityWebRequest.Put(url, data))
            {
                req.timeout = 1;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    logger.Error(Error.SimulatorMRSLocalForwarderFailure,
                        $"Failed to forward join room request to {url}: {req.error}");
                }
            }
        }
    }
}

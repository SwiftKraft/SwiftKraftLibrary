using System.Linq;
using Coherence.Toolkit;
using UnityEngine;

/// <summary>
/// Automatically sets up bridge's client connections at runtime
/// </summary>
/// <remarks>
/// This sample does not ship with any CoherenceSyncConfig, they are generated at import.
/// So instead of assigning a CoherenceSyncConfig reference directly, which is what you would do normally,
/// we try to find it by name and assign it at runtime.
///
/// This assignment must be done before bridge's awake.
/// </remarks>
[DefaultExecutionOrder(ScriptExecutionOrder.CoherenceBridge - 10)]
[RequireComponent(typeof(CoherenceBridge))]
public class SetupClientConnections : MonoBehaviour
{
    public string clientConnectionName;

    private void Awake()
    {
        var clientConfig = CoherenceSyncConfigRegistry.Instance.FirstOrDefault(config => config && config.name == clientConnectionName);
        if (clientConfig && TryGetComponent(out CoherenceBridge bridge))
        {
            bridge.ClientConnectionEntry = clientConfig;
        }
    }
}

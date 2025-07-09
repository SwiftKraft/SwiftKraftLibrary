namespace Coherence.Toolkit
{
    using UnityEngine;

    /// <summary>
    /// Ensures <see cref="CoherenceSyncConfigRegistry.CleanUp"/> gets executed during OnApplicationQuit
    /// even if no coherence bridges exist in the scene.
    /// </summary>
    /// <remarks>
    /// An instance is created at runtime via <see cref="RuntimeInitializeOnLoadMethodAttribute"/>,
    /// and set to <see cref="Object.DontDestroyOnLoad"/>.
    /// </remarks>
    /// <seealso cref="MonoBehaviour"/>
    [DefaultExecutionOrder(ScriptExecutionOrder.OnApplicationQuitSender)]
    internal class OnApplicationQuitSender : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        internal static void InstantiateSender()
        {
            var go = new GameObject(nameof(OnApplicationQuitSender));
            go.AddComponent<OnApplicationQuitSender>();
            DontDestroyOnLoad(go);
        }

        private void OnApplicationQuit()
        {
            // Avoid cleaning up the registry before all CoherenceBridges have been disposed
            // to avoid exceptions during CoherenceSync.CoherenceSyncConfig.Instantiator.Destroy etc.
            // If we don't clean up the registry here, then the last CoherenceBridge for which
            // OnApplicationQuit gets executed last should do it.
            if (CoherenceBridgeStore.bridges.Count is 0)
            {
                CoherenceSyncConfigRegistry.Instance.CleanUp();
            }
        }
    }
}

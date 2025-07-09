namespace Coherence.Toolkit
{
    using Entities;
    using System.Collections.Generic;
    using Logger = Log.Logger;

    public interface ICoherenceSyncUpdater
    {
        Logger logger { get; set; }
        bool TaggedForNetworkedDestruction { get; set; }
        bool ChangedConnection { get; }
        Entity NewConnection { get; }

        void InterpolateBindings();

        void InvokeCallbacks();

        void SyncAndSend();

        void SampleBindings();
        void SampleAllBindings();

        void GetComponentUpdates(List<ICoherenceComponentData> updates, bool forceSerialize = false);

        void PerformInterpolationOnAllBindings();

        void ClearAllSampleTimes();

        void OnConnectedEntityChanged();

        /// <summary>
        ///     Sends changes on all bindings manually instead of waiting for the update.
        ///     External use only when the CoherenceSync behaviour is disabled.
        ///     The sending of the packet containing these changes is still dependant on the packet send rate.
        ///     Do no call before the entity has been registered with the client, which will happen after the
        ///     First update after the client is connected and the CoherenceSync behaviour is enabled.
        ///     If called before the entity is registered with the client updates will be lost.
        /// </summary>
        void ManuallySendAllChanges(bool sampleValuesBeforeSending);
        void SendTag();
        bool TryFlushPosition(bool sampleValueBeforeSending);

        void ApplyComponentDestroys(HashSet<uint> destroyedComponents);

        void ApplyComponentUpdates(ComponentUpdates componentUpdates);
    }
}

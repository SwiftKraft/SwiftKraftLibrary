// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Bindings;
    using Connection;
    using Entities;
    using ProtocolDef;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using UnityEngine;
    using Common;
    using UnityEngine.Events;

    public interface ICoherenceSync
    {
        event UnityAction OnStateAuthority
        {
            add => Warning($"{GetType().Name} does not implement {nameof(OnStateAuthority)}.");
            remove { }
        }

        event UnityAction OnInputAuthority
        {
            add => Warning($"{GetType().Name} does not implement {nameof(OnInputAuthority)}.");
            remove { }
        }

        event UnityAction OnStateRemote
        {
            add => Warning($"{GetType().Name} does not implement {nameof(OnStateRemote)}.");
            remove { }
        }

        UnityEvent OnInputSimulatorConnected => null;

        bool Adopt() => CoherenceBridge.AuthorityManager.Adopt(EntityState);

        bool RequestAuthority(AuthorityType authorityType) => CoherenceBridge.AuthorityManager.RequestAuthority(EntityState, authorityType);

        bool HasStateAuthority => EntityState?.AuthorityType.Value.ControlsState() ?? true;

        bool IsOrphaned => EntityState?.IsOrphaned ?? false;

        bool HasInputAuthority => EntityState?.AuthorityType.Value.ControlsInput() ?? true;

        bool IsSynchronizedWithNetwork => EntityState != null;

        CoherenceSyncConfig CoherenceSyncConfig { get; }

        [MaybeNull]
        NetworkEntityState EntityState { get; }

        CoherenceSync.SimulationType SimulationTypeConfig { get; }
        CoherenceSync.LifetimeType LifetimeTypeConfig { get; }
        CoherenceSync.AuthorityTransferType AuthorityTransferTypeConfig { get; }
        ICoherenceBridge CoherenceBridge { get; }
        ICoherenceSyncUpdater Updater { get; }
        string name { get; }
        Transform transform { get; }
        GameObject gameObject { get; }
        bool PreserveChildren { get; }
        bool HasInput { get; }
        bool UsesLODsAtRuntime { get; }
        string ArchetypeName { get; }
        bool HasParentWithCoherenceSync { get; }
        string ManualUniqueId { get; }
        CoherenceInput Input { get; }
        CoherenceSyncBaked BakedScript { get; }
        Vector3 coherencePosition { get; }
        bool IsUnique { get; }
        bool IsGlobal { get; }
        CoherenceSync.UniqueObjectReplacementStrategy ReplacementStrategy { get; }
        CoherenceSync.UnsyncedNetworkEntityPriority UnsyncedEntityPriority { get; }
        CoherenceSync ConnectedEntity { get; }

        void HandleNetworkedDestruction(bool destroyAsParent);
        void DestroyAsDuplicate();
        void ReceiveCommand(IEntityCommand command, MessageTarget target);
        void HandleDisconnected();
        void ResetInterpolation(bool setToLastSamples = false);
        void SetObservedLodLevel(int lod);
        void RaiseOnAuthorityRequested(AuthorityRequest request);
        bool TryGetBindingByGuid(string bindingGuid, out Binding outBinding);
        void OnNetworkCommandReceived(object sender, byte[] data);
        void InitializeReplacedUniqueObject(SpawnInfo info);
        bool IsChildFromSyncGroup();

        T GetBakedValueBinding<T>(string bindingName = null) where T : Binding;

        CoherenceSync.InterpolationLoop InterpolationLocationConfig { get; }
        CoherenceSync.OrphanedBehavior OrphanedBehaviorConfig { get; }
        List<Binding> Bindings { get; }
        void SendConnectedEntity();
        void ValidateConnectedEntity();
        bool ConnectedEntityChanged(Entity newConnectedEntityID, out bool didChangeParent);
        string CoherenceTag { get; set; }
        void ApplyNodeBindings();

        bool ShouldShift();
        bool ShiftOrigin(Vector3d delta);
        Action<Vector3, Vector3> OnFloatingOriginShifted { get; set; }

        void RaiseOnConnectedEntityChanged();

        private void Warning(string message) => Log.Log.GetLogger(GetType(), this).Warning(Log.Warning.ToolkitInterfaceUnsupported, message);
    }
}

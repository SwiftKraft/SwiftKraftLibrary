// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log
{
    public enum Error
    {
        OperationCanceled,
        BriskUnexpectedAccessOfClosedTransport,
        BriskPingSequenceMissing,
        BriskSendRateExceeded,
        BriskServerConnectionAccept,
        BriskServerConnectionSendMoreThanMTU,
        BriskServerConnectionOutOfSequenceAck,
        BriskServerUpgradeMissingConnection,
        CodeGenError,
        CommonIDisposibleFinalize,
        CoreClientDNSResolveException,
        CoreClientHandlerException,
        CoreClientEntityUpdateUnknownOperation,
        CoreClientEntityUpdateFailed,
        CoreHostNameResolveResult,
        CoreHostNameResolveInnerTask,
        CoreHostNameResolveIPV4,
        CoreChannelOutOrderedNetworkChannelAckNoSent,
        CoreChannelOutOrderedNetworkChannelInvalidAck,
        CoreChannelOutOrderedNetworkChannelSequenceID,
        CoreNativeCoreFactoryCantFindCookie,
        CoreNativeCoreFactoryFailedToRemoveInstance,
        CoreNativeCoreFactoryOnErrorCallback,
        CoreNativeTransportFailedToOpen,
        CoreNativeTransportFailedToClose,
        CoreNativeTransportFailedToSend,
        CoreNativeTransportFailedToReceive,
        CoreNativeTransportFailedToWriteIPAddress,
        CoreNativeTransportFailedToReceiveOnePacket,
        CoreNativeTransportFailedToPrepareDisconnect,
        CoreNativeTransportFailedToConstruct,
        CoreNativeTransportError,
        CoreNativeTransportFailedToAddCookie,
        CoreNativeTransportFailedToGetCookie,
        CoreNetworkTimeExceptionInHandler,
        CoreOutConnectionUpdateOnHeldEntity,
        CoreOutConnectionRemoveComponentHeldEntity,
        DefinitionMissingCommandImplementation,
        DefinitionMissingComponentImplementation,
        DefinitionMissingInputImplementation,
        DeserializeSessionIDFailed,
        EditorBakeUtilAfterCompilationException,
        EditorBakeUtilCompilerErrors,
        EditorBakeUtilEndedException,
        EditorBakeUtilGenerateSchemeException,
        EditorBakeUtilMissingType,
        EditorBakeUtilSchemaNotFound,
        EditorBakeUtilStartException,
        EditorBindingsWindowMaxComponents,
        EditorBindingsWindowMissingBinding,
        EditorBuildPreprocessorMissingSchema,
        EditorBuildPreprocessorFailedToCopySchema,
        EditorHubTrackerIssueError,
        EditorCodegenSelectorSchemaOverride,
        EditorEndpointGet,
        EditorMigratorActiveMigratorsException,
        EditorMigratorAllTargetsException,
        EditorMigratorGenericErrors,
        EditorMigratorSpecificObjectException,
        EditorNetworkPrefabProcessorHasMissingScripts,
        EditorNetworkPrefabProcessorUpdateNullPrefab,
        EditorPortalRequestFeatureNotSupported,
        EditorPortalRequestSDKUnsupported,
        EditorPortalSchemaUploadFail,
        EditorPostProcessorCreatePreloadedException,
        EditorRSBundlerException,
        EditorRSBundlerMissingRS,
        EditorRevisionInfoMissingPackage,
        EditorRevisionInfoRevisionHash,
        EditorSchemaCreatorSaveException,
        EntityManagerBadReasonForDestroy,
        EntityManagerCanNotModify,
        EntityManagerCanNotRemoveComponentExternally,
        EntityManagerCanNotUpdateDetectedCycle,
        EntityManagerCanNotUpdateConnectionScene,
        EntityManagerCanNotUpdateConnectionSceneNotClientconnection,
        EntityManagerCanNotUpdateConnectionSceneMissingParticipant,
        EntityManagerClientCreateClientConnection,
        EntityManagerComponentDataMissing,
        EntityManagerCouldNotAddOrphan,
        EntityManagerCouldNotRemoveOrphan,
        EntityManagerCreateCyclic,
        EntityManagerCreateFailedToDestroyDuplicate,
        EntityManagerCreateInvalid,
        EntityManagerDestroyDoesNotExist,
        EntityManagerDestroyNotOwned,
        EntityManagerDestroyConnectionEntity,
        EntityManagerDuplicateMissing,
        EntityManagerDuplicateUUID,
        EntityManagerEntityNotFound,
        EntityManagerEntityNotOwned,
        EntityManagerFailedToDetectCycle,
        EntityManagerFailedToValidateRemoveComponentRequest,
        EntityManagerModifyNonExistant,
        EntityManagerRemoveInvalidReference,
        EntityManagerRemoveTagFailedNoTag,
        EntityManagerRemoveTagFailedNoEntity,
        EntityManagerRemoveTagFailedNotIndexed,
        EntityManagerRequestInvalidID,
        EntityManagerSceneChangeInvalidParticipant,
        EntityManagerUnauthorizedConnectionUpdate,
        FluxSocketException,
        ReplicationManagerClientWorldFailedToMapChange,
        ReplicationManagerClientWorldFailedToUnmapDestroy,
        ReplicationManagerClientWorldFailedToMapLiveQuery,
        ReplicationManagerClientWorldFailedToMapGlobalQuery,
        ReplicationManagerClientWorldInvalidPositionQueryCenter,
        ReplicationManagerClientWorldInvalidPositionQueryRadius,
        ReplicationManagerClientWorldInvalidTagQuery,
        ReplicationManagerClientWorldInvalidConnectedEntity,
        ReplicationManagerClientWorldMappingFailedForConnectedEntity,
        ReplicationManagerInBufferFailedToMapChange,
        ReplicationManagerInBufferFailedToMapMessage,
        ReplicationManagerInBufferMapAndAppendFailed,
        ReplicationManagerNetworkCloseConnectionDoesNotExist,
        ReplicationManagerNetworkVerifyConnectionInvalid,
        ReplicationManagerNetworkVerifyUnexpectedAuthToken,
        ReplicationManagerOutBufferChangeBufferUnsupported,
        ReplicationManagerOutConnectionFailedToMapEntity,
        ReplicationManagerOutConnectionFailedToSendMessage,
        ReplicationManagerOutConnectionAuthRequestRejectFailed,
        ReplicationManagerOutConnectionSceneIndexChangeFailed,
        ReplicationManagerOutConnectionWorldResultMapFailed,
        ReplicationManagerOutConnectionFailedToGetAck,
        ReplicationManagerPersistenceReadyNoSender,
        ReplicationManagerPersistenceOnlyPCCanSend,
        ReplicationManagerCommandOwnerNotFound,
        ReplicationManagerCommandNotAllowed,
        ReplicationManagerCommandInvalidResponseStatus,
        ReplicationManagerInputEntityNotFound,
        ReplicationManagerInputInvalidResponseStatus,
        ReplicationManagerInvalidSecret,
        ReplicationServerFailedToShutDownTicker,
        ReplicationServerFailedToAutoShutdown,
        ReplicationServerShutdownResourceLeak,
        ReplicationServerFailedToStart,
        RequestManagerOutBufferSentCachePushedNull,
        RuntimeCloudRequestActionNull,
        RuntimeCloudCLIFailedToParseKV,
        RuntimeCloudDeserializationException,
        RuntimeCloudGameServersErrorMsg,
        RuntimeCloudGameServicesKVNotEnabled,
        RuntimeCloudSimulatorMissingToken,
        RuntimeCloudSimulatorAuthToken,
        RuntimeRequestResponse,
        RuntimeRequestError,
        RuntimeRequestFailed,
        RuntimeWebsocketAlreadyConnected,
        RuntimeWebsocketWebError,
        RuntimeWebsocketCloudFailed,
        RuntimeWebsocketOpenFailed,
        RuntimeWebsocketReceiveException,
        RuntimeWebsocketSendFailedNotEnabled,
        RuntimeWebsocketSendException,
        RuntimeWebsocketFailedToDeserializeResponse,
        RuntimeWebsocketMissingResponseCallback,
        RuntimeInvalidCredentials,
        RuntimeInternalException,
        RuntimeServerError,
        RuntimeNotLoggedIn,
        RuntimeAlreadyLoggedIn,
        RuntimeConnectionError,
        RuntimeFeatureDisabled,
        RuntimeOneTimeCodeNotFound,
        RuntimeInvalidConfig,
        RuntimeInvalidApp,
        RuntimeOneTimeCodeExpired,
        RuntimeInvalidResponse,
        RuntimeTooManyRequests,
        RuntimeIdentityLimit,
        RuntimeIdentityNotFound,
        RuntimeIdentityRemoval,
        RuntimeIdentityTaken,
        RuntimeIdentityTotalLimit,
        RuntimeInvalidInput,
        RuntimePasswordNotSet,
        RuntimeUsernameNotAvailable,
        ScribanWriterError,
        ScribanTemplateError,
        SerializeInvalidEntityOperation,
        SerializeSimulationFrameZero,
        ServerTransportHandleSessionID,
        ServerTransportIncomingPacket,
        SimulatorAutoConnectBridge,
        SimulatorAutoConnectDisconnected,
        SimulatorAutoConnectEndpoint,
        SimulatorAutoConnectWorldIDFailed,
        SimulatorAutoConnectCloudFailed,
        SimulatorHTTPListenException,
        SimulatorHTTPSystemException,
        SimulatorMRSLocalForwarderFailure,
        SimulatorMRSStartFailure,
        SimulatorUtilityFailedToAddArgument,
        SpatialIndexFailedToInsert,
        SpatialIndexFailedToUpdate,
        SpatialIndexFailedToRemove,
        StringTooLong,
        SteamFailedToCloseRelay,
        SteamFailedToSendMessage,
        SteamFailedToSendPacket,
        SteamClientNotFound,
        SteamLobbyCreationFailed,
        SteamInternalError,
        SteamP2PConnectionFailed,
        SteamLobbiesRefreshFailed,
        SteamFailedToGetLobbyGameServer,
        ToolkitAuthorityNullState,
        ToolkitBindingDescriptorInvalidType,
        ToolkitBindingDescriptorInvalidCallback,
        ToolkitBindingDescriptorMissingCallback,
        ToolkitBindingDescriptorParamOrder,
        ToolkitBindingIncompatible,
        ToolkitBindingOnValueSyncedException,
        ToolkitBindingMissing,
        ToolkitBindingSpecial,
        ToolkitBindingUnsupported,
        ToolkitBridgeCallbackHandlerException,
        ToolkitBridgeCanNotHandleCommand,
        ToolkitBridgeCanNotHandleInput,
        ToolkitBridgeCodeNotBaked,
        ToolkitBridgeException,
        ToolkitBridgeInvalidEndpoint,
        ToolkitBridgeMasterNotRoot,
        ToolkitBridgeStoreBridgeResolveTooManyCallbacks,
        ToolkitBridgeConnectionError,
        ToolkitClientConnectionManagerFailedToAddConnection,
        ToolkitClientConnectionManagerFailedToRemoveConnection,
        ToolkitCommandBakedSendMissing,
        ToolkitCommandNumCommandBindings,
        ToolkitCommandValidateBindings,
        ToolkitEntitiesManagerDelayedCommands,
        ToolkitEntitiesManagerHandleDisconnected,
        ToolkitEntitiesManagerInstantiationException,
        ToolkitEntitiesManagerPostNetworkCreateException,
        ToolkitInputBakedMethodMissing,
        ToolkitInputBridgeDisconnected,
        ToolkitInputBridgeNotFound,
        ToolkitInputBufferRewrite,
        ToolkitInputBufferAppend,
        ToolkitInputCallbackException,
        ToolkitInputMissingInput,
        ToolkitInputMissingSync,
        ToolkitInputNoReflection,
        ToolkitInputSimulationFailedToRollBack,
        ToolkitInputSimulationTooManySources,
        ToolkitInputDebuggerError,
        ToolKitPlayerLoopInterpolationException,
        ToolkitPlayerLoopReceiveException,
        ToolkitPlayerLoopSamplerException,
        ToolkitPlayerLoopSendException,
        ToolkitPlayerLoopSendSerializerException,
        ToolkitPrefabSyncGroupIDs,
        ToolkitPrefabSyncGroupValidation,
        ToolkitRelayCloseException,
        ToolkitRelayConnection,
        ToolkitRelayOpenException,
        ToolkitRelayRemoveNotFound,
        ToolkitRelayUpdateException,
        ToolkitRelayUpdateFailed,
        ToolkitSceneConnectionDenied,
        ToolkitSceneConnectionError,
        ToolkitSceneFailedToReconnect,
        ToolkitSceneInvalidScene,
        ToolkitSpawnInfoMissingConfigSync,
        ToolkitSyncCommandArgCountMismatch,
        ToolkitSyncCommandArgTypeMismatch,
        ToolkitSyncCommandBindingMissing,
        ToolkitSyncCommandErrorProcessingArg,
        ToolkitSyncCommandGenericArgsTooBig,
        ToolkitSyncCommandGenericByteArgTooLong,
        ToolkitSyncCommandGenericNotFound,
        ToolkitSyncCommandGenericUnsupportedArgType,
        ToolkitSyncCommandInvalidArg,
        ToolkitSyncCommandInvalidName,
        ToolkitSyncCommandInvalidRouting,
        ToolkitSyncCommandInvalidTuple,
        ToolkitSyncCommandNonComponent,
        ToolkitSyncCommandSendException,
        ToolkitSyncCommandUnexpectedBindingType,
        ToolkitSyncComponentActionException,
        ToolkitSyncException,
        ToolkitSyncInstantiationException,
        ToolkitSyncInvalidBridge,
        ToolkitSyncOnAuthoritySameAuthority,
        ToolkitSyncUpdateException,
        ToolkitSyncUpdateInvalidBindingGroup,
        ToolkitSyncValidateSimulationType,
        ToolkitUniquenessUUIDInvalid,
        TransportConnectionClosedUnknown,
        WebInteropOnError,
        WebInteropOnOpen,
        WebInteropOnPacket,
        WebTransportNotSupported,
        StorageOperationError,
        UnobservedError,
        LinkXmlNotFound,
        CopyLinkXmlFailed,
        NlFailedToStartProcess,

        // Special errors for integration tests
        IntegrationTest,
        IntegrationTestRS,
        UnitTestMissingCleanUp,
        BriskClientCantKick,
    }

    public static partial class LogTextMap
    {
        public static string GetText(this Error id)
#pragma warning disable CS8524
            => id switch
#pragma warning restore CS8524
            {
                Error.OperationCanceled => "Operation was canceled by the user.",
                Error.BriskUnexpectedAccessOfClosedTransport => "Unexpected call to closed transport.",
                Error.BriskPingSequenceMissing => "Ping packet sequence ID missing, dropped latency",
                Error.BriskSendRateExceeded => "Expected rate exceeded",
                Error.BriskServerConnectionAccept => "failed to accept connection - wrong state",
                Error.BriskServerConnectionSendMoreThanMTU => "trying to send more data than supported by MTU, dropping packet",
                Error.BriskServerConnectionOutOfSequenceAck => "out of sequence ack.",
                Error.BriskServerUpgradeMissingConnection => "tried to upgrade missing connection",
                Error.CodeGenError => "", // Uses ErrorMsg.
                Error.CoreClientDNSResolveException => "Exception while cancelling dns resolving",
                Error.CommonIDisposibleFinalize => "", // Uses ErrorMSg.
                Error.CoreClientHandlerException => "Exception in handler.",
                Error.CoreClientEntityUpdateUnknownOperation => "Received entity update with unknown operation",
                Error.CoreClientEntityUpdateFailed => "Entity update handling failed",
                Error.CoreHostNameResolveResult => "Failed to resolve host entry with the given hostname, an exception was thrown.",
                Error.CoreHostNameResolveInnerTask => "Failed to resolve host entry with the given hostname, inner task is faulted.",
                Error.CoreHostNameResolveIPV4 => "Failed to resolve host entry with the given hostname, no IPv4 addresses were resolved.",
                Error.CoreChannelOutOrderedNetworkChannelAckNoSent => "OnDelivery Received ACK while there are no sent packets.",
                Error.CoreChannelOutOrderedNetworkChannelInvalidAck => "OnDelivery Received ACK while there are no sent packets.",
                Error.CoreChannelOutOrderedNetworkChannelSequenceID => "OnDelivery dequeued the wrong sequenceID!",
                Error.CoreNativeCoreFactoryCantFindCookie => "Cannot find core instance with given cookie.",
                Error.CoreNativeCoreFactoryFailedToRemoveInstance => "Failed to remove core instance with cookie from the cookie map.",
                Error.CoreNativeCoreFactoryOnErrorCallback => "Native Core Error",
                Error.CoreNativeTransportFailedToOpen => "Failed to open the transport",
                Error.CoreNativeTransportFailedToClose => "Failed to close the transport",
                Error.CoreNativeTransportFailedToSend => "Failed to send on the transport",
                Error.CoreNativeTransportFailedToReceive => "Failed to receive on the transport",
                Error.CoreNativeTransportFailedToWriteIPAddress => "Failed to write IPAddress bytes",
                Error.CoreNativeTransportFailedToReceiveOnePacket => "Failed to prepare disconnect the transport",
                Error.CoreNativeTransportFailedToPrepareDisconnect => "Failed to prepare disconnect the transport",
                Error.CoreNativeTransportFailedToConstruct => "Failed to construct a new transport because the factory is not set.",
                Error.CoreNativeTransportError => "Transport encountered an error",
                Error.CoreNativeTransportFailedToAddCookie => "Failed to add new transport cookie to the cookie map.",
                Error.CoreNativeTransportFailedToGetCookie => "Cannot find transport instance with given cookie.",
                Error.CoreNetworkTimeExceptionInHandler => "Exception in handler.",
                Error.CoreOutConnectionUpdateOnHeldEntity => "UpdateEntity called for entity while it is being held. Will abandon update.",
                Error.CoreOutConnectionRemoveComponentHeldEntity => "RemoveComponent called for entity while it is being held. Will abandon change.",
                Error.DefinitionMissingCommandImplementation => "Missing serialization implementation for a command.",
                Error.DefinitionMissingComponentImplementation => "Missing serialization implementation for a component.",
                Error.DefinitionMissingInputImplementation => "Missing serialization implementation for an input.",
                Error.DeserializeSessionIDFailed => "Failed to extract the session ID.",
                Error.EditorBakeUtilAfterCompilationException => "", // Uses ErrorMsg.
                Error.EditorBakeUtilEndedException => "", // Uses ErrorMsg.
                Error.EditorBakeUtilCompilerErrors => "All compiler errors have to be fixed before you can bake!",
                Error.EditorBakeUtilGenerateSchemeException => "", // Uses ErrorMsg.
                Error.EditorBakeUtilMissingType => "Can not find type",
                Error.EditorBakeUtilSchemaNotFound => "Schema not found in path",
                Error.EditorBakeUtilStartException => "", // Uses ErrorMsg.
                Error.EditorBindingsWindowMaxComponents => "", // Uses ErrorMsg.
                Error.EditorBindingsWindowMissingBinding => "", // Uses ErrorMsg.
                Error.EditorBuildPreprocessorMissingSchema => "", // Uses ErrorMsg.
                Error.EditorBuildPreprocessorFailedToCopySchema => "", // Uses ErrorMsg.
                Error.EditorHubTrackerIssueError => "", // Uses ErrorMsg.
                Error.EditorCodegenSelectorSchemaOverride => "", // Uses ErrorMsg.
                Error.EditorEndpointGet => "", // Uses ErrorMsg.
                Error.EditorMigratorActiveMigratorsException => "", // Uses ErrorMsg.
                Error.EditorMigratorAllTargetsException => "", // Uses ErrorMsg.
                Error.EditorMigratorGenericErrors => "Attempted to reimport coherence Assets but found errors, after the problems have been fixed, try to reimport again through coherence/Reimport coherence Assets menu option.",
                Error.EditorMigratorSpecificObjectException => "", // Uses ErrorMsg.
                Error.EditorNetworkPrefabProcessorUpdateNullPrefab => "Trying to update network prefabs but the given enumerable is null",
                Error.EditorPortalRequestFeatureNotSupported => "", // Uses ErrorMsg.
                Error.EditorPortalRequestSDKUnsupported => "", // Uses ErrorMsg.
                Error.EditorPortalSchemaUploadFail => "", // Uses ErrorMsg.
                Error.EditorPostProcessorCreatePreloadedException => "", // Uses ErrorMsg.
                Error.EditorNetworkPrefabProcessorHasMissingScripts => "", // Uses ErrorMsg.
                Error.EditorRSBundlerException => "", // Uses ErrorMsg.
                Error.EditorRSBundlerMissingRS => "", // Uses ErrorMsg.
                Error.EditorRevisionInfoMissingPackage => "Couldn't find coherence package",
                Error.EditorRevisionInfoRevisionHash => "", // Uses ErrorMsg.
                Error.EditorSchemaCreatorSaveException => "Failed to save bundled schema",
                Error.EntityManagerBadReasonForDestroy => "bad reason for destroy!",
                Error.EntityManagerCanNotModify => "can not modify entity",
                Error.EntityManagerCanNotRemoveComponentExternally => "connection component can not be removed externally",
                Error.EntityManagerCanNotUpdateDetectedCycle => "can not update entity, detected entity cycle",
                Error.EntityManagerCanNotUpdateConnectionScene => "can not update entity connection scene",
                Error.EntityManagerCanNotUpdateConnectionSceneNotClientconnection => "can't update client scene via non-client connection entity",
                Error.EntityManagerCanNotUpdateConnectionSceneMissingParticipant => "missing participant info updating connection scene",
                Error.EntityManagerClientCreateClientConnection => "clients can not create connection entities",
                Error.EntityManagerComponentDataMissing => "component data missing for entity",
                Error.EntityManagerCouldNotAddOrphan => "Could not add orphan",
                Error.EntityManagerCouldNotRemoveOrphan => "Could not remove orphan",
                Error.EntityManagerCreateCyclic => "detected cyclic hierarchy, can not create entity",
                Error.EntityManagerCreateFailedToDestroyDuplicate => "failed to resolve duplicate entity",
                Error.EntityManagerCreateInvalid => "Can not create entity, invalid create request",
                Error.EntityManagerDestroyDoesNotExist => "received destroy for not existing entity",
                Error.EntityManagerDestroyNotOwned => "can't destroy entity: not owned",
                Error.EntityManagerDestroyConnectionEntity => "can not destroy connection entity",
                Error.EntityManagerDuplicateMissing => "could not find original entity that this entity was a duplicate of",
                Error.EntityManagerDuplicateUUID => "can not create two uniquely identified entities with the same UUIDs",
                Error.EntityManagerEntityNotFound => "entity not found",
                Error.EntityManagerEntityNotOwned => "entity not owned or not server owned",
                Error.EntityManagerFailedToDetectCycle => "failed to detect cyclic hierarchy, ID is null",
                Error.EntityManagerFailedToValidateRemoveComponentRequest => "failed to validate component remove request",
                Error.EntityManagerModifyNonExistant => "trying to modify non-existent entity.",
                Error.EntityManagerRemoveInvalidReference => "Trying to remove invalid referenced entity",
                Error.EntityManagerRemoveTagFailedNoTag => "remove tag failed, no tag registered with entity",
                Error.EntityManagerRemoveTagFailedNoEntity => "remove tag failed, no entities registered with tag",
                Error.EntityManagerRemoveTagFailedNotIndexed => "remove tag failed, entity was not indexed with tag",
                Error.EntityManagerRequestInvalidID => "request with invalid entity ID",
                Error.EntityManagerSceneChangeInvalidParticipant => "can not set scene for invalid participant",
                Error.EntityManagerUnauthorizedConnectionUpdate => "can not update entity, unauthorized connection update",
                Error.FluxSocketException => "Socket exception.",
                Error.ReplicationManagerClientWorldFailedToMapChange => "failed to map change",
                Error.ReplicationManagerClientWorldFailedToUnmapDestroy => "failed to unmap destroyed entity",
                Error.ReplicationManagerClientWorldFailedToMapLiveQuery => "failed to map live query entity",
                Error.ReplicationManagerClientWorldFailedToMapGlobalQuery => "failed to map global query entity",
                Error.ReplicationManagerClientWorldInvalidPositionQueryCenter => "invalid position query, null center",
                Error.ReplicationManagerClientWorldInvalidPositionQueryRadius => "invalid position query, null radius",
                Error.ReplicationManagerClientWorldInvalidTagQuery => "tag query has null tag",
                Error.ReplicationManagerClientWorldInvalidConnectedEntity => "Connected Entity is invalid",
                Error.ReplicationManagerClientWorldMappingFailedForConnectedEntity => "mapping failed for entity connectedTo",
                Error.ReplicationManagerInBufferFailedToMapChange => "failed to map entity change data",
                Error.ReplicationManagerInBufferFailedToMapMessage => "failed to map message",
                Error.ReplicationManagerInBufferMapAndAppendFailed => "MapAndAppend - failed to map entity change",
                Error.ReplicationManagerNetworkCloseConnectionDoesNotExist => "close connection does not exist",
                Error.ReplicationManagerNetworkVerifyConnectionInvalid => "attempting to verify challenge on invalid connection.",
                Error.ReplicationManagerNetworkVerifyUnexpectedAuthToken => "unexpected auth token exception",
                Error.ReplicationManagerOutBufferChangeBufferUnsupported => "unsupported change type",
                Error.ReplicationManagerOutConnectionFailedToMapEntity => "failed to map connection entity",
                Error.ReplicationManagerOutConnectionFailedToSendMessage => "failed to send message",
                Error.ReplicationManagerOutConnectionAuthRequestRejectFailed => "authority request rejection failed",
                Error.ReplicationManagerOutConnectionSceneIndexChangeFailed => "scene index changed failed",
                Error.ReplicationManagerOutConnectionWorldResultMapFailed => "worldresult mapping failed",
                Error.ReplicationManagerOutConnectionFailedToGetAck => "failed to get acked data from cache!",
                Error.ReplicationManagerPersistenceReadyNoSender => "persistence ready sender not found",
                Error.ReplicationManagerPersistenceOnlyPCCanSend => "only PC can send persistence ready",
                Error.ReplicationManagerCommandOwnerNotFound => "owner for entity not found",
                Error.ReplicationManagerCommandNotAllowed => "command not allowed",
                Error.ReplicationManagerCommandInvalidResponseStatus => "invalid command response status",
                Error.ReplicationManagerInputEntityNotFound => "entity not found, input will be dropped",
                Error.ReplicationManagerInputInvalidResponseStatus => "invalid input response status",
                Error.ReplicationManagerInvalidSecret => "invalid host room secret",
                Error.ReplicationServerFailedToShutDownTicker => "Failed to shutdown ticker thread",
                Error.ReplicationServerFailedToAutoShutdown => "Failed to autoshutdown thread",
                Error.ReplicationServerShutdownResourceLeak => "", // Uses ErrorMsg.
                Error.ReplicationServerFailedToStart => "Failed to start the Replication Server.",
                Error.RequestManagerOutBufferSentCachePushedNull => "Pushed Null",
                Error.RuntimeCloudRequestActionNull => "Action provided as argument was null.",
                Error.RuntimeCloudCLIFailedToParseKV => "Failed to parse KV",
                Error.RuntimeCloudGameServicesKVNotEnabled => "K/V store not enabled. Visit your developer portal to enable it.",
                Error.RuntimeCloudSimulatorMissingToken => "", // Uses ErrorMsg.
                Error.RuntimeCloudSimulatorAuthToken => "", //  Uses ErrorMsg.
                Error.RuntimeCloudDeserializationException => "Deserialization exception",
                Error.RuntimeCloudGameServersErrorMsg => "", // Uses ErrorMsg.
                Error.RuntimeRequestResponse => "Failed to parse request error body",
                Error.RuntimeRequestError => "Request error",
                Error.RuntimeRequestFailed => "Request failed",
                Error.RuntimeWebsocketAlreadyConnected => "WebSocket: already connected",
                Error.RuntimeWebsocketWebError => "Error",
                Error.RuntimeWebsocketCloudFailed => "Opening WebSocket with the coherence Cloud has failed.",
                Error.RuntimeWebsocketOpenFailed => "Opening socket failed",
                Error.RuntimeWebsocketReceiveException => "Receive exception",
                Error.RuntimeWebsocketSendFailedNotEnabled => "Failed to send request - WebSocket not enabled",
                Error.RuntimeWebsocketSendException => "Send exception",
                Error.RuntimeWebsocketFailedToDeserializeResponse => "Failed to deserialize response",
                Error.RuntimeWebsocketMissingResponseCallback => "Missing response callback",
                Error.RuntimeInvalidCredentials => "Attempted to login using invalid credentials.",
                Error.RuntimeInternalException => "Internal exception.",
                Error.RuntimeServerError => "Server error.",
                Error.RuntimeInvalidResponse => "", // Uses ErrorMsg.
                Error.RuntimeConnectionError => "", // Uses ErrorMsg.
                Error.RuntimeNotLoggedIn => "Operation failed because the user is not logged in.",
                Error.RuntimeAlreadyLoggedIn => "", // Uses ErrorMsg.
                Error.RuntimeTooManyRequests => "", // Uses ErrorMsg.
                Error.RuntimeFeatureDisabled => "", // Uses ErrorMsg.
                Error.RuntimeIdentityNotFound => "", // Uses ErrorMsg.
                Error.RuntimeOneTimeCodeNotFound =>  "", // Uses ErrorMsg.
                Error.RuntimeInvalidConfig =>  "", // Uses ErrorMsg.
                Error.RuntimeInvalidApp =>  "", // Uses ErrorMsg.
                Error.RuntimeOneTimeCodeExpired =>  "", // Uses ErrorMsg.
                Error.RuntimeIdentityLimit => "", // Uses ErrorMsg.
                Error.RuntimeIdentityRemoval => "", // Uses ErrorMsg.
                Error.RuntimeIdentityTaken => "", // Uses ErrorMsg.
                Error.RuntimeIdentityTotalLimit => "", // Uses ErrorMsg.
                Error.RuntimeInvalidInput => "", // Uses ErrorMsg.
                Error.RuntimePasswordNotSet => "", // Uses ErrorMsg.
                Error.RuntimeUsernameNotAvailable => "", // Uses ErrorMsg.
                Error.ScribanWriterError => "", // Uses ErrorMsg
                Error.ScribanTemplateError => "Scriban Template is invalid, check the console for template parsing errors.",
                Error.SerializeInvalidEntityOperation => "Invalid entity operation.",
                Error.SerializeSimulationFrameZero => "Trying to serialize a component with a field simulation frame of 0. This should never happen.",
                Error.ServerTransportHandleSessionID => "Other connection set the session ID.  This is not correct for a server.",
                Error.ServerTransportIncomingPacket => "Handle incoming packet.",
                Error.SimulatorAutoConnectBridge => "Cannot find Bridge in scene. Autosimulator connection will not connect.",
                Error.SimulatorAutoConnectCloudFailed => "", // Uses ErrorMsg
                Error.SimulatorAutoConnectDisconnected => "Disconnected",
                Error.SimulatorAutoConnectEndpoint => "Cannot connect - invalid Endpoint or world information. Make sure you pass the correct Endpoint arguments: --coherence-region --coherence-ip --coherence-port --coherence-room-id --coherence-unique-room-id --coherence-world-id --coherence-http-server-port --coherence-auth-token",
                Error.SimulatorAutoConnectWorldIDFailed => "", // Uses ErrorMsg
                Error.SimulatorHTTPListenException => "", // Uses ErrorMsg
                Error.SimulatorHTTPSystemException => "System exception",
                Error.SimulatorMRSLocalForwarderFailure => "", // Uses ErrorMsg
                Error.SimulatorMRSStartFailure => "", // Uses ErrorMsg
                Error.SimulatorUtilityFailedToAddArgument => "Failed to add an argument. Please check for duplicates",
                Error.SpatialIndexFailedToInsert => "failed to insert, entity already exists",
                Error.SpatialIndexFailedToUpdate => "failed to update, entity does not exist",
                Error.SpatialIndexFailedToRemove => "failed to remove, entity does not exist",
                Error.StringTooLong => "String too long, truncating.",
                Error.SteamFailedToCloseRelay => "Failed to close Steam relay",
                Error.SteamFailedToSendMessage => "Failed to send Steam message",
                Error.SteamFailedToSendPacket => "Failed to send Steam packet",
                Error.SteamClientNotFound => "Steam Client not found",
                Error.SteamLobbyCreationFailed => "Failed to create Steam lobby",
                Error.SteamInternalError => "Steam Internal Error",
                Error.SteamP2PConnectionFailed => "Steam P2P Connection Failed",
                Error.SteamLobbiesRefreshFailed => "Steam lobbies refresh failed",
                Error.SteamFailedToGetLobbyGameServer => "Failed to get Steam lobby game server",
                Error.ToolkitBridgeConnectionError => "CoherenceBridge connection error",
                Error.ToolkitAuthorityNullState => "Cannot do Authority operations for non-synchronized instances.",
                Error.ToolkitBindingDescriptorInvalidType => "", // Uses ErrorMsg
                Error.ToolkitBindingDescriptorInvalidCallback => "", // Uses ErrorMsg
                Error.ToolkitBindingDescriptorMissingCallback => "", // Uses ErrorMsg
                Error.ToolkitBindingDescriptorParamOrder => "Parameter order error",
                Error.ToolkitBindingIncompatible => "", // Uses ErrorMsg.
                Error.ToolkitBindingOnValueSyncedException => "OnValueSynced exception in handler",
                Error.ToolkitBindingMissing => "", // Uses ErrorMsg.
                Error.ToolkitBindingSpecial => "", // Uses ErrorMsg.
                Error.ToolkitBindingUnsupported => "", // Uses ErrorMsg.
                Error.ToolkitBridgeCallbackHandlerException => "exception in handler",
                Error.ToolkitBridgeCanNotHandleCommand => "We can't handle a command for entity, because it doesn't exist.",
                Error.ToolkitBridgeCanNotHandleInput => "We can't handle a input for entity, because it doesn't exist.",
                Error.ToolkitBridgeCodeNotBaked => "Network code not found. Generate it manually via coherence > Bake or enable the Autobaking feature.",
                Error.ToolkitBridgeException => "Bridge exception",
                Error.ToolkitBridgeInvalidEndpoint => "", // Uses ErrorMsg
                Error.ToolkitBridgeMasterNotRoot => "Using Master Bridge option, but we cannot convert this Bridge to a Master via DontDestroyOnLoad. Make sure this Bridge is in the root of the GameObject hierarchy.",
                Error.ToolkitBridgeStoreBridgeResolveTooManyCallbacks => "", // Uses ErrorMsg
                Error.ToolkitClientConnectionManagerFailedToAddConnection => "Failed to add connection",
                Error.ToolkitClientConnectionManagerFailedToRemoveConnection => "Failed to remove connection-to-ClientID mapping",
                Error.ToolkitCommandBakedSendMissing => "", // Uses ErrorMsg
                Error.ToolkitCommandNumCommandBindings => "", // Uses ErrorMsg
                Error.ToolkitCommandValidateBindings => "", // Uses ErrorMsg
                Error.ToolkitEntitiesManagerDelayedCommands => "failed to find network entity when applying delayed commands.",
                Error.ToolkitEntitiesManagerHandleDisconnected => "", // Uses ErrorMsg
                Error.ToolkitEntitiesManagerInstantiationException => "", // Uses ErrorMsg
                Error.ToolkitEntitiesManagerPostNetworkCreateException => "", // Uses ErrorMsg
                Error.ToolkitInputBakedMethodMissing => "", // Uses ErrorMsg
                Error.ToolkitInputBridgeDisconnected => "", // Uses ErrorMsg
                Error.ToolkitInputBridgeNotFound => "Bridge not found. Did you forget to add one to the scene? (`coherence -> Scene Setup -> Create CoherenceBridge`)",
                Error.ToolkitInputBufferRewrite => "Trying to rewrite buffer with `frame > LastFrame`",
                Error.ToolkitInputBufferAppend => "Trying to append input with `frame <= LastFrame`",
                Error.ToolkitInputCallbackException => "Callback exception",
                Error.ToolkitInputMissingInput => "", // Uses ErrorMsg
                Error.ToolkitInputMissingSync => "", // Uses ErrorMsg
                Error.ToolkitInputNoReflection => "", // Uses ErrorMsg
                Error.ToolkitInputSimulationFailedToRollBack => "Failed to rollback state store",
                Error.ToolkitInputSimulationTooManySources => "", // Uses ErrorMsg
                Error.ToolkitInputDebuggerError => "", // Uses ErrorMsg
                Error.ToolKitPlayerLoopInterpolationException => "", // Uses ErrorMsg
                Error.ToolkitPlayerLoopReceiveException => "", // Uses ErrorMsg
                Error.ToolkitPlayerLoopSamplerException => "", // Uses ErrorMsg
                Error.ToolkitPlayerLoopSendException => "", // Uses ErrorMsg
                Error.ToolkitPlayerLoopSendSerializerException => "", // Uses ErrorMsg
                Error.ToolkitPrefabSyncGroupIDs => "Too many ids sent for prefab group, groups do not match.",
                Error.ToolkitPrefabSyncGroupValidation => "", // Uses ErrorMsg
                Error.ToolkitRelayCloseException => "", // Uses ErrorMsg
                Error.ToolkitRelayConnection => "Connection error",
                Error.ToolkitRelayOpenException => "", // Uses ErrorMsg
                Error.ToolkitRelayRemoveNotFound => "Connection not found",
                Error.ToolkitRelayUpdateException => "", // Uses ErrorMsg
                Error.ToolkitRelayUpdateFailed => "Update failed, RS connection is closed",
                Error.ToolkitSceneConnectionDenied => "Replicator denied connection to local simulator",
                Error.ToolkitSceneConnectionError => "Local simulator connection error",
                Error.ToolkitSceneFailedToReconnect => "Failed to reconnect",
                Error.ToolkitSceneInvalidScene => "Invalid scene",
                Error.ToolkitSpawnInfoMissingConfigSync => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandArgCountMismatch => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandArgTypeMismatch => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandBindingMissing => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandErrorProcessingArg => "error processing command arguments",
                Error.ToolkitSyncCommandGenericArgsTooBig => "Command arguments are too big to be sent.",
                Error.ToolkitSyncCommandGenericByteArgTooLong => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandGenericNotFound => "Received generic command but the binding wasn't found.",
                Error.ToolkitSyncCommandGenericUnsupportedArgType => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandInvalidArg => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandInvalidName => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandInvalidRouting => "Sending message with invalid routing.",
                Error.ToolkitSyncCommandInvalidTuple => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandNonComponent => "Cannot send a command to a non-component",
                Error.ToolkitSyncCommandSendException => "", // Uses ErrorMsg
                Error.ToolkitSyncCommandUnexpectedBindingType => "", // Uses ErrorMsg
                Error.ToolkitSyncComponentActionException => "", // Uses ErrorMsg
                Error.ToolkitSyncInstantiationException => "", // Uses ErrorMsg
                Error.ToolkitSyncInvalidBridge => "", // Uses ErrorMsg
                Error.ToolkitSyncException => "Sync exception",
                Error.ToolkitSyncOnAuthoritySameAuthority => "", // Uses ErrorMsg
                Error.ToolkitSyncUpdateException => "", // Uses ErrorMsg
                Error.ToolkitSyncUpdateInvalidBindingGroup => "", // Uses ErrorMsg
                Error.ToolkitSyncValidateSimulationType => "", // Uses ErrorMsg
                Error.ToolkitUniquenessUUIDInvalid => "", // Uses ErrorMsg
                Error.TransportConnectionClosedUnknown => "Tried to close unknown connection.",
                Error.WebInteropOnError => "OnError",
                Error.WebInteropOnOpen => "OnOpen",
                Error.WebInteropOnPacket => "OnPacket",
                Error.WebTransportNotSupported => "Can't use WebConnection in a non-WebGL client.",
                Error.UnobservedError => "", // Uses ErrorMsg.
                Error.StorageOperationError => "", // Uses ErrorMsg.
                Error.LinkXmlNotFound => "", // Uses ErrorMsg
                Error.CopyLinkXmlFailed => "", // Uses ErrorMsg
                Error.NlFailedToStartProcess => "Failed to start process.",
                Error.UnitTestMissingCleanUp => "", // Uses ErrorMsg
                Error.BriskClientCantKick => "Only a simulator or a host can kick other clients. The kick request was not sent.",

                // Special errors for integration tests.
                Error.IntegrationTest => "", // Uses ErrorMsg.
                Error.IntegrationTestRS => "", // Uses ErrorMsg.
            };
    }
}

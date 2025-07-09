// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct InteropStructSizes
    {
        public Int32 ClientID_Size;
        public Int32 AbsoluteSimulationFrame_Size;

        public Int32 AuthorityRequestRejection_ID_Offset;
        public Int32 AuthorityRequestRejection_ID_Size;
        public Int32 AuthorityRequestRejection_AuthType_Offset;
        public Int32 AuthorityRequestRejection_AuthType_Size;
        public Int32 AuthorityRequestRejection_Size;

        public Int32 AuthorityChange_ID_Offset;
        public Int32 AuthorityChange_ID_Size;
        public Int32 AuthorityChange_AuthType_Offset;
        public Int32 AuthorityChange_AuthType_Size;
        public Int32 AuthorityChange_Size;

        public Int32 SceneIndexChange_EntityId_Offset;
        public Int32 SceneIndexChange_EntityId_Size;
        public Int32 SceneIndexChange_SceneIndex_Offset;
        public Int32 SceneIndexChange_SceneIndex_Size;
        public Int32 SceneIndexChange_Size;

        public Int32 AuthorityRequest_ID_Offset;
        public Int32 AuthorityRequest_ID_Size;
        public Int32 AuthorityRequest_RequesterID_Offset;
        public Int32 AuthorityRequest_RequesterID_Size;
        public Int32 AuthorityRequest_AuthType_Offset;
        public Int32 AuthorityRequest_AuthType_Size;
        public Int32 AuthorityRequest_Size;

        public Int32 OutgoingEntityUpdate_ID_Offset;
        public Int32 OutgoingEntityUpdate_ID_Size;
        public Int32 OutgoingEntityUpdate_Components_Offset;
        public Int32 OutgoingEntityUpdate_Components_Size;
        public Int32 OutgoingEntityUpdate_ComponentCount_Offset;
        public Int32 OutgoingEntityUpdate_ComponentCount_Size;
        public Int32 OutgoingEntityUpdate_DestroyedComponents_Offset;
        public Int32 OutgoingEntityUpdate_DestroyedComponents_Size;
        public Int32 OutgoingEntityUpdate_DestroyedCount_Offset;
        public Int32 OutgoingEntityUpdate_DestroyedCount_Size;
        public Int32 OutgoingEntityUpdate_Priority_Offset;
        public Int32 OutgoingEntityUpdate_Priority_Size;
        public Int32 OutgoingEntityUpdate_Operation_Offset;
        public Int32 OutgoingEntityUpdate_Operation_Size;
        public Int32 OutgoingEntityUpdate_Size;

        public Int32 ComponentDataContainer_ComponentID_Offset;
        public Int32 ComponentDataContainer_ComponentID_Size;
        public Int32 ComponentDataContainer_FieldMask_Offset;
        public Int32 ComponentDataContainer_FieldMask_Size;
        public Int32 ComponentDataContainer_StoppedMask_Offset;
        public Int32 ComponentDataContainer_StoppedMask_Size;
        public Int32 ComponentDataContainer_Data_Offset;
        public Int32 ComponentDataContainer_Data_Size;
        public Int32 ComponentDataContainer_DataSize_Offset;
        public Int32 ComponentDataContainer_DataSize_Size;
        public Int32 ComponentDataContainer_SimFrames_Offset;
        public Int32 ComponentDataContainer_SimFrames_Size;
        public Int32 ComponentDataContainer_SimFrameCount_Offset;
        public Int32 ComponentDataContainer_SimFrameCount_Size;
        public Int32 ComponentDataContainer_Size;

        public Int32 EntityMessageContainer_Entity_Offset;
        public Int32 EntityMessageContainer_Entity_Size;
        public Int32 EntityMessageContainer_Data_Offset;
        public Int32 EntityMessageContainer_Data_Size;
        public Int32 EntityMessageContainer_DataSize_Offset;
        public Int32 EntityMessageContainer_DataSize_Size;
        public Int32 EntityMessageContainer_Size;

        public Int32 ByteArray_Data_Offset;
        public Int32 ByteArray_Data_Size;
        public Int32 ByteArray_Length_Offset;
        public Int32 ByteArray_Length_Size;
        public Int32 ByteArray_Size;

        public Int32 PingSettings_MinSamplesForStability_Offset;
        public Int32 PingSettings_MinSamplesForStability_Size;
        public Int32 PingSettings_MaxStableDeviation_Offset;
        public Int32 PingSettings_MaxStableDeviation_Size;
        public Int32 PingSettings_MaxSamples_Offset;
        public Int32 PingSettings_MaxSamples_Size;
        public Int32 PingSettings_Size;

        public Int32 Ping_AverageLatencyMs_Offset;
        public Int32 Ping_AverageLatencyMs_Size;
        public Int32 Ping_IsStable_Offset;
        public Int32 Ping_IsStable_Size;
        public Int32 Ping_LatestLatencyMs_Offset;
        public Int32 Ping_LatestLatencyMs_Size;
        public Int32 Ping_Size;

        public Int32 ConnectionSettings_PingSettings_Offset;
        public Int32 ConnectionSettings_PingSettings_Size;
        public Int32 ConnectionSettings_DisconnectTimeoutMilliseconds_Offset;
        public Int32 ConnectionSettings_DisconnectTimeoutMilliseconds_Size;
        public Int32 ConnectionSettings_UseDebugStreams_Offset;
        public Int32 ConnectionSettings_UseDebugStreams_Size;
        public Int32 ConnectionSettings_Size;

        public Int32 Entity_Index_Offset;
        public Int32 Entity_Index_Size;
        public Int32 Entity_Version_Offset;
        public Int32 Entity_Version_Size;
        public Int32 Entity_Type_Offset;
        public Int32 Entity_Type_Size;
        public Int32 Entity_Size;

        public Int32 EndpointData_Host_Offset;
        public Int32 EndpointData_Host_Size;
        public Int32 EndpointData_Port_Offset;
        public Int32 EndpointData_Port_Size;
        public Int32 EndpointData_AuthToken_Offset;
        public Int32 EndpointData_AuthToken_Size;
        public Int32 EndpointData_RuntimeKey_Offset;
        public Int32 EndpointData_RuntimeKey_Size;
        public Int32 EndpointData_RoomId_Offset;
        public Int32 EndpointData_RoomId_Size;
        public Int32 EndpointData_UniqueRoomId_Offset;
        public Int32 EndpointData_UniqueRoomId_Size;
        public Int32 EndpointData_WorldId_Offset;
        public Int32 EndpointData_WorldId_Size;
        public Int32 EndpointData_Region_Offset;
        public Int32 EndpointData_Region_Size;
        public Int32 EndpointData_SchemaId_Offset;
        public Int32 EndpointData_SchemaId_Size;
        public Int32 EndpointData_SimulatorType_Offset;
        public Int32 EndpointData_SimulatorType_Size;
        public Int32 EndpointData_RoomSecret_Offset;
        public Int32 EndpointData_RoomSecret_Size;
        public Int32 EndpointData_RSVersion_Offset;
        public Int32 EndpointData_RSVersion_Size;
        public Int32 EndpointData_CustomLocalToken_Offset;
        public Int32 EndpointData_CustomLocalToken_Size;
        public Int32 EndpointData_Size;

        public Int32 Vector3d_X_Offset;
        public Int32 Vector3d_X_Size;
        public Int32 Vector3d_Y_Offset;
        public Int32 Vector3d_Y_Size;
        public Int32 Vector3d_Z_Offset;
        public Int32 Vector3d_Z_Size;
        public Int32 Vector3d_Size;

        public Int32 Vector3f_X_Offset;
        public Int32 Vector3f_X_Size;
        public Int32 Vector3f_Y_Offset;
        public Int32 Vector3f_Y_Size;
        public Int32 Vector3f_Z_Offset;
        public Int32 Vector3f_Z_Size;
        public Int32 Vector3f_Size;

        public Int32 NetworkConditions_SendDelaySec_Offset;
        public Int32 NetworkConditions_SendDelaySec_Size;
        public Int32 NetworkConditions_SendDropRate_Offset;
        public Int32 NetworkConditions_SendDropRate_Size;
        public Int32 NetworkConditions_ReceiveDelaySec_Offset;
        public Int32 NetworkConditions_ReceiveDelaySec_Size;
        public Int32 NetworkConditions_ReceiveDropRate_Offset;
        public Int32 NetworkConditions_ReceiveDropRate_Size;
        public Int32 NetworkConditions_Size;

        public Int32 EntityWithMeta_EntityId_Offset;
        public Int32 EntityWithMeta_EntityId_Size;
        public Int32 EntityWithMeta_HasMeta_Offset;
        public Int32 EntityWithMeta_HasMeta_Size;
        public Int32 EntityWithMeta_HasStateAuthority_Offset;
        public Int32 EntityWithMeta_HasStateAuthority_Size;
        public Int32 EntityWithMeta_HasInputAuthority_Offset;
        public Int32 EntityWithMeta_HasInputAuthority_Size;
        public Int32 EntityWithMeta_IsOrphan_Offset;
        public Int32 EntityWithMeta_IsOrphan_Size;
        public Int32 EntityWithMeta_LOD_Offset;
        public Int32 EntityWithMeta_LOD_Size;
        public Int32 EntityWithMeta_Operation_Offset;
        public Int32 EntityWithMeta_Operation_Size;
        public Int32 EntityWithMeta_DestroyReason_Offset;
        public Int32 EntityWithMeta_DestroyReason_Size;
        public Int32 EntityWithMeta_Size;

        public Int32 CoherenceContextInitResult_Context_Offset;
        public Int32 CoherenceContextInitResult_Context_Size;
        public Int32 CoherenceContextInitResult_ErrorCode_Offset;
        public Int32 CoherenceContextInitResult_ErrorCode_Size;
        public Int32 CoherenceContextInitResult_Size;
    }

    internal static partial class NativeWrapper
    {
        internal static unsafe InteropStructSizes GetInteropStructSizes()
        {
            var authorityRequestRejection = new InteropAuthorityRequestRejection();
            var authorityChange = new InteropAuthorityChange();
            var sceneIndexChange = new InteropSceneIndexChange();
            var authorityRequest = new InteropAuthorityRequest();
            var outgoingEntityUpdate = new InteropOutgoingEntityUpdate();
            var componentDataContainer = new ComponentDataContainer();
            var entityMessageContainer = new EntityMessageContainer();
            var byteArray = new ByteArray();
            var pingSettings = new InteropPingSettings();
            var ping = new InteropPing();
            var connectionSettings = new InteropConnectionSettings();
            var entity = new InteropEntity();
            var endpointData = new InteropEndpointData()
            {
                Host = ""
            };
            var vector3d = new InteropVector3d();
            var vector3f = new InteropVector3f();
            var networkConditions = new InteropNetworkConditions();
            var entityWithMeta = new InteropEntityWithMeta();
            var coherenceContextInitResult = new CoherenceContextInitResult();

            var r = new InteropStructSizes();

            r.ClientID_Size = Marshal.SizeOf<InteropClientID>();
            r.AbsoluteSimulationFrame_Size = Marshal.SizeOf<InteropAbsoluteSimulationFrame>();

            r.AuthorityRequestRejection_ID_Offset = Marshal.OffsetOf<InteropAuthorityRequestRejection>(nameof(InteropAuthorityRequestRejection.ID)).ToInt32();
            r.AuthorityRequestRejection_ID_Size = Marshal.SizeOf(authorityRequestRejection.ID);
            r.AuthorityRequestRejection_AuthType_Offset = Marshal.OffsetOf<InteropAuthorityRequestRejection>(nameof(InteropAuthorityRequestRejection.AuthType)).ToInt32();
            r.AuthorityRequestRejection_AuthType_Size = Marshal.SizeOf(Enum.GetUnderlyingType(authorityRequestRejection.AuthType.GetType()));
            r.AuthorityRequestRejection_Size = Marshal.SizeOf(authorityRequestRejection);

            r.AuthorityChange_ID_Offset = Marshal.OffsetOf<InteropAuthorityChange>(nameof(InteropAuthorityChange.ID)).ToInt32();
            r.AuthorityChange_ID_Size = Marshal.SizeOf(authorityChange.ID);
            r.AuthorityChange_AuthType_Offset = Marshal.OffsetOf<InteropAuthorityChange>(nameof(InteropAuthorityChange.AuthType)).ToInt32();
            r.AuthorityChange_AuthType_Size = Marshal.SizeOf(Enum.GetUnderlyingType(authorityChange.AuthType.GetType()));
            r.AuthorityChange_Size = Marshal.SizeOf(authorityChange);

            r.SceneIndexChange_EntityId_Offset = Marshal.OffsetOf<InteropSceneIndexChange>(nameof(InteropSceneIndexChange.EntityId)).ToInt32();
            r.SceneIndexChange_EntityId_Size = Marshal.SizeOf(sceneIndexChange.EntityId);
            r.SceneIndexChange_SceneIndex_Offset = Marshal.OffsetOf<InteropSceneIndexChange>(nameof(InteropSceneIndexChange.SceneIndex)).ToInt32();
            r.SceneIndexChange_SceneIndex_Size = Marshal.SizeOf(sceneIndexChange.SceneIndex);
            r.SceneIndexChange_Size = Marshal.SizeOf(sceneIndexChange);

            r.AuthorityRequest_ID_Offset = Marshal.OffsetOf<InteropAuthorityRequest>(nameof(InteropAuthorityRequest.ID)).ToInt32();
            r.AuthorityRequest_ID_Size = Marshal.SizeOf(authorityRequest.ID);
            r.AuthorityRequest_RequesterID_Offset = Marshal.OffsetOf<InteropAuthorityRequest>(nameof(InteropAuthorityRequest.RequesterID)).ToInt32();
            r.AuthorityRequest_RequesterID_Size = Marshal.SizeOf(authorityRequest.RequesterID);
            r.AuthorityRequest_AuthType_Offset = Marshal.OffsetOf<InteropAuthorityRequest>(nameof(InteropAuthorityRequest.AuthType)).ToInt32();
            r.AuthorityRequest_AuthType_Size = Marshal.SizeOf(Enum.GetUnderlyingType(authorityRequest.AuthType.GetType()));
            r.AuthorityRequest_Size = Marshal.SizeOf(authorityRequest);

            r.OutgoingEntityUpdate_ID_Offset = Marshal.OffsetOf<InteropOutgoingEntityUpdate>(nameof(InteropOutgoingEntityUpdate.ID)).ToInt32();
            r.OutgoingEntityUpdate_ID_Size = Marshal.SizeOf(outgoingEntityUpdate.ID);
            r.OutgoingEntityUpdate_Components_Offset = Marshal.OffsetOf<InteropOutgoingEntityUpdate>(nameof(InteropOutgoingEntityUpdate.Components)).ToInt32();
            r.OutgoingEntityUpdate_Components_Size = Marshal.SizeOf(new IntPtr(outgoingEntityUpdate.Components));
            r.OutgoingEntityUpdate_ComponentCount_Offset = Marshal.OffsetOf<InteropOutgoingEntityUpdate>(nameof(InteropOutgoingEntityUpdate.ComponentCount)).ToInt32();
            r.OutgoingEntityUpdate_ComponentCount_Size = Marshal.SizeOf(outgoingEntityUpdate.ComponentCount);
            r.OutgoingEntityUpdate_DestroyedComponents_Offset = Marshal.OffsetOf<InteropOutgoingEntityUpdate>(nameof(InteropOutgoingEntityUpdate.DestroyedComponents)).ToInt32();
            r.OutgoingEntityUpdate_DestroyedComponents_Size = Marshal.SizeOf(new IntPtr(outgoingEntityUpdate.DestroyedComponents));
            r.OutgoingEntityUpdate_DestroyedCount_Offset = Marshal.OffsetOf<InteropOutgoingEntityUpdate>(nameof(InteropOutgoingEntityUpdate.DestroyedCount)).ToInt32();
            r.OutgoingEntityUpdate_DestroyedCount_Size = Marshal.SizeOf(outgoingEntityUpdate.DestroyedCount);
            r.OutgoingEntityUpdate_Priority_Offset = Marshal.OffsetOf<InteropOutgoingEntityUpdate>(nameof(InteropOutgoingEntityUpdate.Priority)).ToInt32();
            r.OutgoingEntityUpdate_Priority_Size = Marshal.SizeOf(outgoingEntityUpdate.Priority);
            r.OutgoingEntityUpdate_Operation_Offset = Marshal.OffsetOf<InteropOutgoingEntityUpdate>(nameof(InteropOutgoingEntityUpdate.Operation)).ToInt32();
            r.OutgoingEntityUpdate_Operation_Size = Marshal.SizeOf(Enum.GetUnderlyingType(outgoingEntityUpdate.Operation.GetType()));
            r.OutgoingEntityUpdate_Size = Marshal.SizeOf(outgoingEntityUpdate);

            r.ComponentDataContainer_ComponentID_Offset = Marshal.OffsetOf<ComponentDataContainer>(nameof(ComponentDataContainer.ComponentID)).ToInt32();
            r.ComponentDataContainer_ComponentID_Size = Marshal.SizeOf(componentDataContainer.ComponentID);
            r.ComponentDataContainer_FieldMask_Offset = Marshal.OffsetOf<ComponentDataContainer>(nameof(ComponentDataContainer.FieldMask)).ToInt32();
            r.ComponentDataContainer_FieldMask_Size = Marshal.SizeOf(componentDataContainer.FieldMask);
            r.ComponentDataContainer_StoppedMask_Offset = Marshal.OffsetOf<ComponentDataContainer>(nameof(ComponentDataContainer.StoppedMask)).ToInt32();
            r.ComponentDataContainer_StoppedMask_Size = Marshal.SizeOf(componentDataContainer.StoppedMask);
            r.ComponentDataContainer_Data_Offset = Marshal.OffsetOf<ComponentDataContainer>(nameof(ComponentDataContainer.Data)).ToInt32();
            r.ComponentDataContainer_Data_Size = Marshal.SizeOf(componentDataContainer.Data);
            r.ComponentDataContainer_DataSize_Offset = Marshal.OffsetOf<ComponentDataContainer>(nameof(ComponentDataContainer.DataSize)).ToInt32();
            r.ComponentDataContainer_DataSize_Size = Marshal.SizeOf(componentDataContainer.DataSize);
            r.ComponentDataContainer_SimFrames_Offset = Marshal.OffsetOf<ComponentDataContainer>(nameof(ComponentDataContainer.SimFrames)).ToInt32();
            r.ComponentDataContainer_SimFrames_Size = Marshal.SizeOf(new IntPtr(componentDataContainer.SimFrames));
            r.ComponentDataContainer_SimFrameCount_Offset = Marshal.OffsetOf<ComponentDataContainer>(nameof(ComponentDataContainer.SimFrameCount)).ToInt32();
            r.ComponentDataContainer_SimFrameCount_Size = Marshal.SizeOf(componentDataContainer.SimFrameCount);
            r.ComponentDataContainer_Size = Marshal.SizeOf(componentDataContainer);

            r.EntityMessageContainer_Entity_Offset = Marshal.OffsetOf<EntityMessageContainer>(nameof(EntityMessageContainer.Entity)).ToInt32();
            r.EntityMessageContainer_Entity_Size = Marshal.SizeOf(entityMessageContainer.Entity);
            r.EntityMessageContainer_Data_Offset = Marshal.OffsetOf<EntityMessageContainer>(nameof(EntityMessageContainer.Data)).ToInt32();
            r.EntityMessageContainer_Data_Size = Marshal.SizeOf(entityMessageContainer.Data);
            r.EntityMessageContainer_DataSize_Offset = Marshal.OffsetOf<EntityMessageContainer>(nameof(EntityMessageContainer.DataSize)).ToInt32();
            r.EntityMessageContainer_DataSize_Size = Marshal.SizeOf(entityMessageContainer.DataSize);
            r.EntityMessageContainer_Size = Marshal.SizeOf(entityMessageContainer);

            r.ByteArray_Data_Offset = Marshal.OffsetOf<ByteArray>(nameof(ByteArray.Data)).ToInt32();
            r.ByteArray_Data_Size = Marshal.SizeOf(new IntPtr(byteArray.Data));
            r.ByteArray_Length_Offset = Marshal.OffsetOf<ByteArray>(nameof(ByteArray.Length)).ToInt32();
            r.ByteArray_Length_Size = Marshal.SizeOf(byteArray.Length);
            r.ByteArray_Size = Marshal.SizeOf(byteArray);

            r.PingSettings_MinSamplesForStability_Offset = Marshal.OffsetOf<InteropPingSettings>(nameof(InteropPingSettings.MinSamplesForStability)).ToInt32();
            r.PingSettings_MinSamplesForStability_Size = Marshal.SizeOf(pingSettings.MinSamplesForStability);
            r.PingSettings_MaxStableDeviation_Offset = Marshal.OffsetOf<InteropPingSettings>(nameof(InteropPingSettings.MaxStableDeviation)).ToInt32();
            r.PingSettings_MaxStableDeviation_Size = Marshal.SizeOf(pingSettings.MaxStableDeviation);
            r.PingSettings_MaxSamples_Offset = Marshal.OffsetOf<InteropPingSettings>(nameof(InteropPingSettings.MaxSamples)).ToInt32();
            r.PingSettings_MaxSamples_Size = Marshal.SizeOf(pingSettings.MaxSamples);
            r.PingSettings_Size = Marshal.SizeOf(pingSettings);

            r.Ping_AverageLatencyMs_Offset = Marshal.OffsetOf<InteropPing>(nameof(InteropPing.AverageLatencyMs)).ToInt32();
            r.Ping_AverageLatencyMs_Size = Marshal.SizeOf(ping.AverageLatencyMs);
            r.Ping_IsStable_Offset = Marshal.OffsetOf<InteropPing>(nameof(InteropPing.IsStable)).ToInt32();
            r.Ping_IsStable_Size = Marshal.SizeOf(ping.IsStable);
            r.Ping_LatestLatencyMs_Offset = Marshal.OffsetOf<InteropPing>(nameof(InteropPing.LatestLatencyMs)).ToInt32();
            r.Ping_LatestLatencyMs_Size = Marshal.SizeOf(ping.LatestLatencyMs);
            r.Ping_Size = Marshal.SizeOf(ping);

            r.ConnectionSettings_PingSettings_Offset = Marshal.OffsetOf<InteropConnectionSettings>(nameof(InteropConnectionSettings.PingSettings)).ToInt32();
            r.ConnectionSettings_PingSettings_Size = Marshal.SizeOf(connectionSettings.PingSettings);
            r.ConnectionSettings_DisconnectTimeoutMilliseconds_Offset = Marshal.OffsetOf<InteropConnectionSettings>(nameof(InteropConnectionSettings.DisconnectTimeoutMilliseconds)).ToInt32();
            r.ConnectionSettings_DisconnectTimeoutMilliseconds_Size = Marshal.SizeOf(connectionSettings.DisconnectTimeoutMilliseconds);
            r.ConnectionSettings_UseDebugStreams_Offset = Marshal.OffsetOf<InteropConnectionSettings>(nameof(InteropConnectionSettings.UseDebugStreams)).ToInt32();
            r.ConnectionSettings_UseDebugStreams_Size = Marshal.SizeOf(connectionSettings.UseDebugStreams);
            r.ConnectionSettings_Size = Marshal.SizeOf(connectionSettings);

            r.Entity_Index_Offset = Marshal.OffsetOf<InteropEntity>(nameof(InteropEntity.Index)).ToInt32();
            r.Entity_Index_Size = Marshal.SizeOf(entity.Index);
            r.Entity_Version_Offset = Marshal.OffsetOf<InteropEntity>(nameof(InteropEntity.Version)).ToInt32();
            r.Entity_Version_Size = Marshal.SizeOf(entity.Version);
            r.Entity_Type_Offset = Marshal.OffsetOf<InteropEntity>(nameof(InteropEntity.Type)).ToInt32();
            r.Entity_Type_Size = Marshal.SizeOf(Enum.GetUnderlyingType(entity.Type.GetType()));
            r.Entity_Size = Marshal.SizeOf(entity);

            r.EndpointData_Host_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.Host)).ToInt32();
            r.EndpointData_Host_Size = InteropEndpointData.HostMaxLength;
            r.EndpointData_Port_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.Port)).ToInt32();
            r.EndpointData_Port_Size = Marshal.SizeOf(endpointData.Port);
            r.EndpointData_AuthToken_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.AuthToken)).ToInt32();
            r.EndpointData_AuthToken_Size = InteropEndpointData.AuthTokenMaxLength;
            r.EndpointData_RuntimeKey_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.RuntimeKey)).ToInt32();
            r.EndpointData_RuntimeKey_Size = InteropEndpointData.RuntimeKeyMaxLength;
            r.EndpointData_RoomId_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.RoomId)).ToInt32();
            r.EndpointData_RoomId_Size = Marshal.SizeOf(endpointData.RoomId);
            r.EndpointData_UniqueRoomId_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.UniqueRoomId)).ToInt32();
            r.EndpointData_UniqueRoomId_Size = Marshal.SizeOf(endpointData.UniqueRoomId);
            r.EndpointData_WorldId_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.WorldId)).ToInt32();
            r.EndpointData_WorldId_Size = Marshal.SizeOf(endpointData.WorldId);
            r.EndpointData_Region_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.Region)).ToInt32();
            r.EndpointData_Region_Size = InteropEndpointData.RegionMaxLength;
            r.EndpointData_SchemaId_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.SchemaId)).ToInt32();
            r.EndpointData_SchemaId_Size = InteropEndpointData.SchemaIdMaxLength;
            r.EndpointData_SimulatorType_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.SimulatorType)).ToInt32();
            r.EndpointData_SimulatorType_Size = InteropEndpointData.SimulatorTypeMaxLength;
            r.EndpointData_RoomSecret_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.RoomSecret)).ToInt32();
            r.EndpointData_RoomSecret_Size = InteropEndpointData.RoomSecretMaxLength;
            r.EndpointData_RSVersion_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.RSVersion)).ToInt32();
            r.EndpointData_RSVersion_Size = InteropEndpointData.RsVersionMaxLength;
            r.EndpointData_CustomLocalToken_Offset = Marshal.OffsetOf<InteropEndpointData>(nameof(InteropEndpointData.CustomLocalToken)).ToInt32();
            r.EndpointData_CustomLocalToken_Size = Marshal.SizeOf(endpointData.CustomLocalToken);
            r.EndpointData_Size = Marshal.SizeOf(endpointData);

            r.Vector3d_X_Offset = Marshal.OffsetOf<InteropVector3d>(nameof(InteropVector3d.X)).ToInt32();
            r.Vector3d_X_Size = Marshal.SizeOf(vector3d.X);
            r.Vector3d_Y_Offset = Marshal.OffsetOf<InteropVector3d>(nameof(InteropVector3d.Y)).ToInt32();
            r.Vector3d_Y_Size = Marshal.SizeOf(vector3d.Y);
            r.Vector3d_Z_Offset = Marshal.OffsetOf<InteropVector3d>(nameof(InteropVector3d.Z)).ToInt32();
            r.Vector3d_Z_Size = Marshal.SizeOf(vector3d.Z);
            r.Vector3d_Size = Marshal.SizeOf(vector3d);

            r.Vector3f_X_Offset = Marshal.OffsetOf<InteropVector3f>(nameof(InteropVector3f.X)).ToInt32();
            r.Vector3f_X_Size = Marshal.SizeOf(vector3f.X);
            r.Vector3f_Y_Offset = Marshal.OffsetOf<InteropVector3f>(nameof(InteropVector3f.Y)).ToInt32();
            r.Vector3f_Y_Size = Marshal.SizeOf(vector3f.Y);
            r.Vector3f_Z_Offset = Marshal.OffsetOf<InteropVector3f>(nameof(InteropVector3f.Z)).ToInt32();
            r.Vector3f_Z_Size = Marshal.SizeOf(vector3f.Z);
            r.Vector3f_Size = Marshal.SizeOf(vector3f);

            r.NetworkConditions_SendDelaySec_Offset = Marshal.OffsetOf<InteropNetworkConditions>(nameof(InteropNetworkConditions.SendDelaySec)).ToInt32();
            r.NetworkConditions_SendDelaySec_Size = Marshal.SizeOf(networkConditions.SendDelaySec);
            r.NetworkConditions_SendDropRate_Offset = Marshal.OffsetOf<InteropNetworkConditions>(nameof(InteropNetworkConditions.SendDropRate)).ToInt32();
            r.NetworkConditions_SendDropRate_Size = Marshal.SizeOf(networkConditions.SendDropRate);
            r.NetworkConditions_ReceiveDelaySec_Offset = Marshal.OffsetOf<InteropNetworkConditions>(nameof(InteropNetworkConditions.ReceiveDelaySec)).ToInt32();
            r.NetworkConditions_ReceiveDelaySec_Size = Marshal.SizeOf(networkConditions.ReceiveDelaySec);
            r.NetworkConditions_ReceiveDropRate_Offset = Marshal.OffsetOf<InteropNetworkConditions>(nameof(InteropNetworkConditions.ReceiveDropRate)).ToInt32();
            r.NetworkConditions_ReceiveDropRate_Size = Marshal.SizeOf(networkConditions.ReceiveDropRate);
            r.NetworkConditions_Size = Marshal.SizeOf(networkConditions);

            r.EntityWithMeta_EntityId_Offset = Marshal.OffsetOf<InteropEntityWithMeta>(nameof(InteropEntityWithMeta.EntityId)).ToInt32();
            r.EntityWithMeta_EntityId_Size = Marshal.SizeOf(entityWithMeta.EntityId);
            r.EntityWithMeta_HasMeta_Offset = Marshal.OffsetOf<InteropEntityWithMeta>(nameof(InteropEntityWithMeta.HasMeta)).ToInt32();
            r.EntityWithMeta_HasMeta_Size = Marshal.SizeOf(entityWithMeta.HasMeta);
            r.EntityWithMeta_HasStateAuthority_Offset = Marshal.OffsetOf<InteropEntityWithMeta>(nameof(InteropEntityWithMeta.HasStateAuthority)).ToInt32();
            r.EntityWithMeta_HasStateAuthority_Size = Marshal.SizeOf(entityWithMeta.HasStateAuthority);
            r.EntityWithMeta_HasInputAuthority_Offset = Marshal.OffsetOf<InteropEntityWithMeta>(nameof(InteropEntityWithMeta.HasInputAuthority)).ToInt32();
            r.EntityWithMeta_HasInputAuthority_Size = Marshal.SizeOf(entityWithMeta.HasInputAuthority);
            r.EntityWithMeta_IsOrphan_Offset = Marshal.OffsetOf<InteropEntityWithMeta>(nameof(InteropEntityWithMeta.IsOrphan)).ToInt32();
            r.EntityWithMeta_IsOrphan_Size = Marshal.SizeOf(entityWithMeta.IsOrphan);
            r.EntityWithMeta_LOD_Offset = Marshal.OffsetOf<InteropEntityWithMeta>(nameof(InteropEntityWithMeta.LOD)).ToInt32();
            r.EntityWithMeta_LOD_Size = Marshal.SizeOf(entityWithMeta.LOD);
            r.EntityWithMeta_Operation_Offset = Marshal.OffsetOf<InteropEntityWithMeta>(nameof(InteropEntityWithMeta.Operation)).ToInt32();
            r.EntityWithMeta_Operation_Size = Marshal.SizeOf(Enum.GetUnderlyingType(entityWithMeta.Operation.GetType()));
            r.EntityWithMeta_DestroyReason_Offset = Marshal.OffsetOf<InteropEntityWithMeta>(nameof(InteropEntityWithMeta.DestroyReason)).ToInt32();
            r.EntityWithMeta_DestroyReason_Size = Marshal.SizeOf(Enum.GetUnderlyingType(entityWithMeta.DestroyReason.GetType()));
            r.EntityWithMeta_Size = Marshal.SizeOf(entityWithMeta);

            r.CoherenceContextInitResult_Context_Offset = Marshal.OffsetOf<CoherenceContextInitResult>(nameof(CoherenceContextInitResult.Context)).ToInt32();
            r.CoherenceContextInitResult_Context_Size = Marshal.SizeOf(new IntPtr(coherenceContextInitResult.Context));
            r.CoherenceContextInitResult_ErrorCode_Offset = Marshal.OffsetOf<CoherenceContextInitResult>(nameof(CoherenceContextInitResult.ErrorCode)).ToInt32();
            r.CoherenceContextInitResult_ErrorCode_Size = Marshal.SizeOf(Enum.GetUnderlyingType(coherenceContextInitResult.ErrorCode.GetType()));
            r.CoherenceContextInitResult_Size = Marshal.SizeOf(coherenceContextInitResult);

            return r;
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Simulator
{
    using Coherence.Cloud;
    using Coherence.Connection;

    [System.Serializable]
    public class JoinRoomRequest
    {
        public RoomData room;
        public string rsVersion;

        public static JoinRoomRequest FromEndpointData(EndpointData endpointData)
        {
            return new JoinRoomRequest
            {
                rsVersion = endpointData.rsVersion,
                room = new RoomData() {
                    Id = endpointData.roomId,
                    UniqueId = endpointData.uniqueRoomId,
                    Host = new RoomHostData()
                    {
                        Ip = endpointData.host,
                        Port = endpointData.port,
                        Region = endpointData.region,
                    },
                },
            };
        }

        public EndpointData ToEndpointData()
        {
            // can't be used safely at editor time as is, only at runtime
            var rs = RuntimeSettings.Instance;
            return new EndpointData
            {
                host = room.Host.Ip,
                port = room.Host.Port,
                roomId = room.Id,
                uniqueRoomId = room.UniqueId,
                region = room.Host.Region,
                schemaId = rs.SchemaID,
                runtimeKey = rs.RuntimeKey,
                rsVersion = rsVersion,
            };
        }
    }
}

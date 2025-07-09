namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;

    public class RoomsCache
    {
        public event Action<string, List<RoomData>> OnRoomsUpdated;

        private string endpoint;

        private List<RoomData> rooms = new List<RoomData>();

        public IReadOnlyList<RoomData> CachedRooms => rooms;

        public RoomsCache(string endpoint)
        {
            this.endpoint = endpoint;
        }

        internal void ClearRooms()
        {
            rooms.Clear();

            OnRoomsUpdated?.Invoke(endpoint, rooms);
        }

        internal void AddRoom(RoomData room)
        {
            rooms.Add(room);

            OnRoomsUpdated?.Invoke(endpoint, rooms);
        }

        internal void PopulateRooms(List<RoomData> roomsToAdd)
        {
            rooms.AddRange(roomsToAdd);

            OnRoomsUpdated?.Invoke(endpoint, rooms);
        }

        internal void RemoveRoom(ulong uniqueId)
        {
            var roomToBeRemoved = rooms.FindIndex(room => room.UniqueId == uniqueId || room.Id == uniqueId);

            if (roomToBeRemoved < 0)
            {
                return;
            }

            rooms.RemoveAt(roomToBeRemoved);

            OnRoomsUpdated?.Invoke(endpoint, rooms);
        }

    }
}

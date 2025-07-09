// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
// Any changes to the Unity version of the request should be reflected
// in the HttpClient version.
// TODO: Separate Http client impl. with common options/policy layer (coherence/unity#1764)
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Common;
    using Log;
    using Newtonsoft.Json;
    using Runtime;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Utils;
    using Logger = Log.Logger;

    /// <summary>
    /// Public API to interface with Rooms from a self-hosted Replication Server.
    /// If you wish to create, delete or fetch rooms in the coherence Cloud, you can instantiate
    /// a <see cref="CloudService" /> instead to access the CloudRooms API.
    /// </summary>
    public class ReplicationServerRoomsService : IRoomsService, IDisposable
    {
        private readonly IRequestFactory requestFactory;
        private readonly RoomsCache roomsCache;
        private readonly IRuntimeSettings runtimeSettings;
        private readonly Logger logger = Log.GetLogger<ReplicationServerRoomsService>();

        private readonly string roomsResolveEndpoint = "/rooms";
        private readonly string localRoomsGetMethod = "/get";
        private readonly string localRoomsCreateMethod = "/add";
        private readonly string localRoomsDeleteMethod = "/remove";

        private string localRoomsIp;
        private int localRoomsApiPort;
        private string endpoint;

        private List<Action<RequestResponse<IReadOnlyList<RoomData>>>> fetchRoomsCallbackList = new List<Action<RequestResponse<IReadOnlyList<RoomData>>>>();

        private bool isFetchingRooms;
        private readonly bool shouldDisposeRequestFactory;

        /// <summary>
        /// List of cached Rooms that were fetched by the FetchRooms method.
        /// </summary>
        public IReadOnlyList<RoomData> CachedRooms => roomsCache.CachedRooms;

        public ReplicationServerRoomsService(string ip = null, int? apiPort = null, IRequestFactory requestFactory = null, IRuntimeSettings runtimeSettings = null)
        {
            this.runtimeSettings = runtimeSettings;
#if UNITY
            this.runtimeSettings ??= RuntimeSettings.Instance;
#endif

            localRoomsIp = !string.IsNullOrEmpty(ip) ? ip : this.runtimeSettings.LocalHost;
            localRoomsApiPort = apiPort ?? this.runtimeSettings.APIPort;

            endpoint = $"http://{localRoomsIp}:{localRoomsApiPort}";

            if (requestFactory is null)
            {
                shouldDisposeRequestFactory = true;
                requestFactory = new RequestFactory(this.runtimeSettings, false);
            }

            this.requestFactory = requestFactory;

            this.roomsCache = new RoomsCache(endpoint);
        }

        /// <returns>Returns true if the targeted Replication Server is online, false otherwise.</returns>
        public async Task<bool> IsOnline() => await ReplicationServerUtils.PingHttpServerAsync(localRoomsIp, localRoomsApiPort);

        /// <summary>Remove an existing room that you have created in the Replication Server.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="roomId">Room ID of the room you wish to remove. Can be found in RoomData.Id</param>
        /// <param name="secret">Secret is ignored for removing rooms from locally hosted Replication Servers</param>
        public void RemoveRoom(ulong roomId, string secret, Action<RequestResponse<string>> onRequestFinished)
        {
            var body = CoherenceJson.SerializeObject(new RemoveRoomRequest() { RoomId = (ushort)roomId });

            requestFactory.SendCustomRequest(endpoint, localRoomsDeleteMethod, "POST", body, response =>
            {
                var requestResponse = RequestResponse<string>.GetRequestResponse(response);

                if (requestResponse.Status == RequestStatus.Fail)
                {
                    onRequestFinished?.Invoke(requestResponse);
                    return;
                }

                roomsCache.RemoveRoom(roomId);

                onRequestFinished?.Invoke(requestResponse);
            });
        }

        /// <summary>Remove an existing room that you have created in the Replication Server asynchronously.</summary>
        /// <param name="roomId">Room ID of the room you wish to remove. Can be found in RoomData.Id</param>
        /// <param name="secret">Secret is ignored for removing rooms from locally hosted Replication Servers</param>
        public async Task RemoveRoomAsync(ulong roomId, string secret)
        {
            var body = CoherenceJson.SerializeObject(new RemoveRoomRequest() { RoomId = (ushort)roomId });

            var task = requestFactory.SendCustomRequestAsync(endpoint, localRoomsDeleteMethod, "POST", body);

            _ = await task;

            if (task.Exception != null)
            {
                throw task.Exception;
            }

            roomsCache.RemoveRoom(roomId);
        }

        /// <summary>Create a room with the given options in the Replication Server.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished, it includes the created room.</param>
        /// <param name="roomCreationOptions">Instance of all the room options that you wish to create the room with.</param>
        public void CreateRoom(Action<RequestResponse<RoomData>> onRequestFinished, RoomCreationOptions roomCreationOptions)
        {
            SelfHostedRoomCreationOptions castedRoomCreationOptions = GetCastedRoomCreationOptions(roomCreationOptions);

            string requestBody = GetLocalRoomCreationRequestBody(castedRoomCreationOptions);

            logger.Trace("CreateRoom - start", ("endpoint", endpoint), ("maxClients", castedRoomCreationOptions.MaxClients), ("tags", string.Join(",", castedRoomCreationOptions.Tags ?? new string[] { })), ("findOrCreate", false));

            requestFactory.SendCustomRequest(endpoint, localRoomsCreateMethod, "POST", requestBody, response =>
            {
                var requestResponse = RequestResponse<RoomData>.GetRequestResponse(response);

                if (requestResponse.Status == RequestStatus.Fail)
                {
                    onRequestFinished?.Invoke(requestResponse);
                    return;
                }

                try
                {
                    var roomData = GetLocalRoomDataFromResponse(castedRoomCreationOptions, response.Result);

                    logger.Trace("CreateRoom - end", ("roomID", roomData.Id));

                    roomsCache.AddRoom(roomData);

                    requestResponse.Result = roomData;
                }
                catch (Exception ex)
                {
                    requestResponse.Status = RequestStatus.Fail;
                    requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, ex.Message);

                    logger.Error(Error.RuntimeCloudDeserializationException,
                        ("Request", nameof(CreateRoom)),
                        ("Response", response.Result),
                        ("exception", ex));

                    throw new ResponseDeserializationException(Result.InvalidResponse, ex.Message);
                }
                finally
                {
                    onRequestFinished?.Invoke(requestResponse);
                }
            });
        }

        /// <summary>Create a room with the given options in the Replication Server asynchronously.</summary>
        /// <param name="roomCreationOptions">Instance of all the room options that you wish to create the room with.</param>
        public async Task<RoomData> CreateRoomAsync(RoomCreationOptions roomCreationOptions)
        {
            SelfHostedRoomCreationOptions castedRoomCreationOptions = GetCastedRoomCreationOptions(roomCreationOptions);

            if (castedRoomCreationOptions.FindOrCreate)
            {
                var rooms = await FetchRoomsAsync(roomCreationOptions.Tags);
                if (rooms.Count > 0)
                {
                    return rooms[0];
                }
            }

            var requestBody = GetLocalRoomCreationRequestBody(castedRoomCreationOptions);

            logger.Trace("CreateRoom - start", ("endpoint", endpoint), ("maxClients", castedRoomCreationOptions.MaxClients), ("tags", string.Join(",", castedRoomCreationOptions.Tags ?? new string[] { })), ("findOrCreate", castedRoomCreationOptions.FindOrCreate));

            var text = await requestFactory.SendCustomRequestAsync(endpoint, localRoomsCreateMethod, "POST", requestBody);

            try
            {
                RoomData newRoom = GetLocalRoomDataFromResponse(castedRoomCreationOptions, text);

                logger.Trace("CreateRoom - end", ("roomID", newRoom.Id));

                roomsCache.AddRoom(newRoom);

                return newRoom;
            }
            catch (Exception ex)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(CreateRoomAsync)),
                    ("Response", text),
                    ("exception", ex));

                throw new ResponseDeserializationException(Result.InvalidResponse, ex.Message);
            }
        }

        /// <summary>Fetch the available rooms in the Replication Server.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished, it includes the fetched rooms.</param>
        /// <param name="tags">Filter the results by a list of tags.</param>
        public void FetchRooms(Action<RequestResponse<IReadOnlyList<RoomData>>> onRequestFinished, string[] tags = null)
        {
            if (WaitForOngoingRequest(onRequestFinished))
            {
                return;
            }

            roomsCache.ClearRooms();

            logger.Trace("FetchRooms - start", ("endpoint", endpoint));

            requestFactory.SendCustomRequest(endpoint, localRoomsGetMethod,
                "GET", null, response =>
                {
                    var requestResponse = RequestResponse<IReadOnlyList<RoomData>>.GetRequestResponse(response);

                    if (requestResponse.Status == RequestStatus.Fail)
                    {
                        requestResponse.Result = new List<RoomData>();
                        foreach (var callback in fetchRoomsCallbackList)
                        {
                            callback?.Invoke(requestResponse);
                        }
                        fetchRoomsCallbackList.Clear();
                        return;
                    }

                    try
                    {
                        var rooms = OnFetchLocal(response.Result, tags);

                        logger.Trace("FetchRooms - end", ("rooms count", rooms.Count));

                        roomsCache.PopulateRooms(rooms);

                        requestResponse.Result = rooms;
                    }
                    catch (Exception ex)
                    {
                        requestResponse.Status = RequestStatus.Fail;
                        requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, ex.Message);

                        logger.Error(Error.RuntimeCloudDeserializationException,
                            ("Request", nameof(FetchRooms)),
                            ("Response", requestResponse.Result),
                            ("exception", ex));

                    }
                    finally
                    {
                        foreach (var callback in fetchRoomsCallbackList)
                        {
                            callback?.Invoke(requestResponse);
                        }

                        fetchRoomsCallbackList.Clear();
                    }
                });
        }

        /// <summary>Fetch the available rooms in the Replication Server asynchronously.</summary>
        /// <param name="tags">Filter the results by a list of tags.</param>
        public async Task<IReadOnlyList<RoomData>> FetchRoomsAsync(string[] tags = null)
        {
            await AwaitForPreviousRequestAsync();

            roomsCache.ClearRooms();

            logger.Trace("FetchRooms - start", ("endpoint", endpoint));

            string response = "";

            try
            {
                response = await requestFactory.SendCustomRequestAsync(endpoint, localRoomsGetMethod,
                    "GET", null);
            }
            finally
            {
                isFetchingRooms = false;
            }

            try
            {
                var rooms = OnFetchLocal(response, tags);

                logger.Trace("FetchRooms - end", ("rooms count", rooms.Count));

                roomsCache.PopulateRooms(rooms);

                return rooms;
            }
            catch (Exception ex)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(FetchRoomsAsync)),
                    ("Response", Result.InvalidResponse),
                    ("exception", ex));

                throw new ResponseDeserializationException(Result.InvalidResponse, ex.Message);
            }
            finally
            {
                isFetchingRooms = false;
            }
        }

        private SelfHostedRoomCreationOptions GetCastedRoomCreationOptions(RoomCreationOptions roomCreationOptions)
        {
            var castedRoomCreationOptions = roomCreationOptions as SelfHostedRoomCreationOptions;

            if (castedRoomCreationOptions == null)
            {
                logger.Debug(
                    $"{nameof(ReplicationServerRoomsService)} expects a {nameof(SelfHostedRoomCreationOptions)} instance. Using the given instance of {nameof(roomCreationOptions)} instead.");
                castedRoomCreationOptions = SelfHostedRoomCreationOptions.FromRoomCreationOptions(roomCreationOptions);
            }

            return castedRoomCreationOptions;
        }

        private async Task AwaitForPreviousRequestAsync()
        {
            while (isFetchingRooms)
            {
                await Task.Yield();
            }

            isFetchingRooms = true;
        }

        private bool WaitForOngoingRequest(Action<RequestResponse<IReadOnlyList<RoomData>>> onRequestFinished)
        {
            fetchRoomsCallbackList.Add(onRequestFinished);

            return fetchRoomsCallbackList.Count > 1;
        }

        private List<RoomData> OnFetchLocal(string text, string[] tags)
        {
            LocalRoomsResponse response = Utils.CoherenceJson.DeserializeObject<LocalRoomsResponse>(text);
            var result = response.Rooms.Where(item =>
            {
                if (tags != null && tags.Length > 0)
                {
                    return !tags.Except(item.Tags).Any();
                }

                return true;
            }).Select(room => new RoomData
            {
                Id = room.ID,
                UniqueId = room.UniqueID,
                MaxPlayers = room.MaxClients,
                ConnectedPlayers = room.ConnectionCount,
                Tags = room.Tags,
                KV = room.KVP,
                Host = new RoomHostData
                {
                    Ip = localRoomsIp,
                    Port = runtimeSettings.IsWebGL ? runtimeSettings.LocalRoomsWebPort : runtimeSettings.LocalRoomsUDPPort,
                    Region = "local"
                }
            });

            return result.ToList();
        }

        private string GetRemoveRoomPath(string pathParams)
        {
            return $"{roomsResolveEndpoint}{pathParams}";
        }

        private RoomData GetLocalRoomDataFromResponse(SelfHostedRoomCreationOptions roomCreationOptions, string text)
        {
            var roomData = Utils.CoherenceJson.DeserializeObject<LocalRoomData>(text);
            RoomData newRoom = new RoomData
            {
                Id = roomData.RoomID,
                UniqueId = (ulong)roomCreationOptions.UniqueId,
                Secret = roomData.Secret,
                Host = new RoomHostData
                {
                    Ip = localRoomsIp,
                    Port = runtimeSettings.IsWebGL ? runtimeSettings.LocalRoomsWebPort : runtimeSettings.LocalRoomsUDPPort,
                    Region = "local",
                },
                KV = roomCreationOptions.KeyValues,
                MaxPlayers = roomCreationOptions.MaxClients,
                ConnectedPlayers = 0,
                Tags = roomCreationOptions.Tags,
            };
            return newRoom;
        }

        private static string GetLocalRoomCreationRequestBody(SelfHostedRoomCreationOptions roomCreationOptions)
        {
            var requestBody = Utils.CoherenceJson.SerializeObject(roomCreationOptions.ToRequest());
            return requestBody;
        }

        public void Dispose()
        {
            if (shouldDisposeRequestFactory)
            {
                ((IDisposable)requestFactory).Dispose();
            }

            logger.Dispose();
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Common;
    using Log;
    using Runtime;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using Logger = Log.Logger;

    /// <summary>
    /// Public API to interface with Rooms from your Project in the coherence Cloud.
    /// If you wish to create, delete or fetch rooms in a self-hosted Replication Server, you can instantiate
    /// a <see cref="ReplicationServerRoomsService" /> instead.
    /// </summary>
    public class CloudRoomsService : IRoomsService, IAsyncDisposable, IDisposable
    {
        private IRequestFactory requestFactory;
        private RoomsCache roomsCache;

        private IAuthClientInternal authClient;
        private readonly IRuntimeSettings runtimeSettings;
        private readonly Logger logger = Log.GetLogger<CloudRoomsService>();

        private readonly string roomsResolveEndpoint = "/rooms";

        private List<Action<RequestResponse<IReadOnlyList<RoomData>>>> fetchRoomsCallbackList = new List<Action<RequestResponse<IReadOnlyList<RoomData>>>>();

        private bool isFetchingRooms;

        private string region;
        private bool shouldDisposeRequestFactoryAndAuthClient;

        /// <summary>
        /// List of cached Rooms that were fetched by the FetchRooms method.
        /// </summary>
        public IReadOnlyList<RoomData> CachedRooms => roomsCache.CachedRooms;

        public CloudRoomsService(string region, CloudCredentialsPair credentialsPair, IRuntimeSettings runtimeSettings) : this(region, credentialsPair, runtimeSettings, null)  { }

        internal CloudRoomsService(string region, [MaybeNull] CloudCredentialsPair credentialsPair, [MaybeNull] IRuntimeSettings runtimeSettings, [MaybeNull] IPlayerAccountProvider playerAccountProvider)
        {
#if UNITY
            runtimeSettings ??= RuntimeSettings.Instance;
#endif
            this.region = region;
            this.runtimeSettings = runtimeSettings;

            if (credentialsPair is null)
            {
                shouldDisposeRequestFactoryAndAuthClient = true;
                credentialsPair = CloudCredentialsFactory.ForClient(runtimeSettings, playerAccountProvider:playerAccountProvider);
                credentialsPair.authClient.LoginAsGuest().Then(task => logger.Warning(Warning.RuntimeCloudLoginFailedMsg, task.Exception.ToString()), TaskContinuationOptions.OnlyOnFaulted);
            }

            this.requestFactory = credentialsPair.RequestFactory;
            this.authClient = credentialsPair.authClient;
            this.roomsCache = new RoomsCache(region);
        }

        /// <returns>Returns the internal cooldown for the Remove Room endpoint.</returns>
        public TimeSpan GetRemoveRoomCooldown()
        {
            return requestFactory.GetRequestCooldown(roomsResolveEndpoint, "DELETE");
        }

        /// <summary>Remove an existing room that you have created in the coherence Cloud.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="uniqueID">Unique ID of the room you wish to remove. Can be found in RoomData.UniqueId</param>
        /// <param name="secret">Token that will allow you to remove the room. Can be found in RoomData.Secret</param>
        public void RemoveRoom(ulong uniqueID, string secret, Action<RequestResponse<string>> onRequestFinished)
        {
            var pathParams = $"/{uniqueID}/{secret}";

            logger.Trace("RemoveRoom", ("endpoint", roomsResolveEndpoint), ("path", pathParams));

            requestFactory.SendRequest(roomsResolveEndpoint, pathParams, "DELETE", null, null, $"{nameof(CloudRoomsService)}.{nameof(RemoveRoom)}", authClient.SessionToken, response =>
            {
                var requestResponse = RequestResponse<string>.GetRequestResponse(response);

                if (requestResponse.Status == RequestStatus.Fail)
                {
                    onRequestFinished?.Invoke(requestResponse);
                    return;
                }

                roomsCache.RemoveRoom(uniqueID);
                onRequestFinished?.Invoke(requestResponse);
            });
        }



        /// <summary>Unlist an existing room that you have created in the coherence Cloud.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="uniqueID">Unique ID of the room you wish to unlist. Can be found in RoomData.UniqueId</param>
        /// <param name="secret">Token that will allow you to unlist the room. Can be found in RoomData.Secret</param>
        public void UnlistRoom(ulong uniqueID, string secret, Action<RequestResponse<string>> onRequestFinished)
        {
            var pathParams = $"/{uniqueID}/unlist";

            var requestBody = Utils.CoherenceJson.SerializeObject(new RoomUnlistRequest()
            {
                Secret = secret
            });

            logger.Trace("UnlistRoom", ("endpoint", roomsResolveEndpoint), ("path", pathParams), ("body", requestBody));

            requestFactory.SendRequest(roomsResolveEndpoint, pathParams, "PATCH", requestBody, null, $"{nameof(CloudRoomsService)}.{nameof(UnlistRoom)}", authClient.SessionToken, response =>
            {
                var requestResponse = RequestResponse<string>.GetRequestResponse(response);

                if (requestResponse.Status == RequestStatus.Fail)
                {
                    onRequestFinished?.Invoke(requestResponse);
                    return;
                }

                onRequestFinished?.Invoke(requestResponse);
            });
        }

        /// <summary>Remove an existing room that you have created in the coherence Cloud.</summary>
        /// <param name="uniqueID">Unique ID of the room you wish to remove. Can be found in RoomData.UniqueId</param>
        /// <param name="secret">Token that will allow you to remove the room. Can be found in RoomData.Secret</param>
        public async Task RemoveRoomAsync(ulong uniqueID, string secret)
        {
            var pathParams = $"/{uniqueID}/{secret}";

            logger.Trace("RemoveRoom", ("endpoint", roomsResolveEndpoint), ("path", pathParams));

            var task = requestFactory.SendRequestAsync(roomsResolveEndpoint, pathParams, "DELETE", null, null, $"{nameof(CloudRoomsService)}.{nameof(RemoveRoomAsync)}", authClient.SessionToken);

            await task;

            if (task.Exception != null)
            {
                throw task.Exception;
            }

            roomsCache.RemoveRoom(uniqueID);
        }

        /// <summary>Unlist an existing room that you have created in the coherence Cloud.</summary>
        /// <param name="uniqueID">Unique ID of the room you wish to unlist. Can be found in RoomData.UniqueId</param>
        /// <param name="secret">Token that will allow you to unlist the room. Can be found in RoomData.Secret</param>
        public async Task UnlistRoomAsync(ulong uniqueID, string secret)
        {
            var pathParams = $"/{uniqueID}/unlist";

            var requestBody = Utils.CoherenceJson.SerializeObject(new RoomUnlistRequest()
            {
                Secret = secret
            });

            logger.Trace("UnlistRoom", ("endpoint", roomsResolveEndpoint), ("path", pathParams));

            var task = requestFactory.SendRequestAsync(roomsResolveEndpoint, pathParams, "PATCH", requestBody, null, $"{nameof(CloudRoomsService)}.{nameof(UnlistRoomAsync)}", authClient.SessionToken);

            await task;

            if (task.Exception != null)
            {
                throw task.Exception;
            }
        }

        /// <returns>Returns the internal cooldown for the Create Room endpoint.</returns>
        public TimeSpan GetCreateRoomCooldown()
        {
            return requestFactory.GetRequestCooldown(roomsResolveEndpoint, "POST");
        }

        /// <summary>Create a room with the given options in the coherence Cloud.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished, it includes the created room.</param>
        /// <param name="roomCreationOptions">Instance of all the room options that you wish to create the room with.</param>
        public void CreateRoom(Action<RequestResponse<RoomData>> onRequestFinished, RoomCreationOptions roomCreationOptions)
        {
            string requestBody = GetRoomCreationRequestBody(roomCreationOptions);

            logger.Trace("CreateRoom - start", ("region", region), ("endpoint", roomsResolveEndpoint), ("maxClients", roomCreationOptions.MaxClients), ("tags", string.Join(",", roomCreationOptions.Tags ?? new string[] { })), ("findOrCreate", false));

            requestFactory.SendRequest(roomsResolveEndpoint, "POST", requestBody, null, $"{nameof(CloudRoomsService)}.{nameof(CreateRoom)}", authClient.SessionToken,
            response =>
            {
                var requestResponse = RequestResponse<RoomData>.GetRequestResponse(response);

                if (requestResponse.Status == RequestStatus.Fail)
                {
                    onRequestFinished?.Invoke(requestResponse);
                    return;
                }

                try
                {
                    RoomData room = DeserializeCreatedRoom(response.Result);

                    requestResponse.Result = room;
                }
                catch (Exception exception)
                {
                    requestResponse.Status = RequestStatus.Fail;
                    requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, exception.Message);

                    logger.Error(Error.RuntimeCloudDeserializationException,
                        ("Request", nameof(CreateRoom)),
                        ("Response", response.Result),
                        ("exception", exception));
                }
                finally
                {
                    onRequestFinished?.Invoke(requestResponse);
                }
            });
        }

        /// <summary>Create a room with the given options in the coherence Cloud asynchronously.</summary>
        /// <param name="roomCreationOptions">Instance of all the room options that you wish to create the room with.</param>
        public async Task<RoomData> CreateRoomAsync(RoomCreationOptions roomCreationOptions)
        {
            var requestBodyForRegion = GetRoomCreationRequestBody(roomCreationOptions);

            logger.Trace("CreateRoom - start", ("region", region), ("endpoint", roomsResolveEndpoint), ("maxClients", roomCreationOptions.MaxClients), ("tags", string.Join(",", roomCreationOptions.Tags ?? new string[] { })), ("findOrCreate", roomCreationOptions.FindOrCreate));

            var textResponse = await requestFactory.SendRequestAsync(roomsResolveEndpoint, "POST", requestBodyForRegion, null, $"{nameof(CloudRoomsService)}.{nameof(CreateRoomAsync)}", authClient.SessionToken);

            try
            {
                RoomData room = DeserializeCreatedRoom(textResponse);

                return room;
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(CreateRoomAsync)),
                    ("Response", textResponse),
                    ("exception", exception));

                throw new ResponseDeserializationException(Result.InvalidResponse, exception.Message);
            }
        }

        /// <summary>Match against a joinable room with the given options in the coherence Cloud.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished, it includes the matched room or null.</param>
        /// <param name="tags">Filter the results by a list of tags.</param>
        public void MatchRoom(Action<RequestResponse<RoomMatchResponse>> onRequestFinished, string[] tags = null)
        {
            var requestBody = Utils.CoherenceJson.SerializeObject(new RoomMatchRequest()
            {
                Tags = tags ?? new string[] { },
                Region = region,
                SimSlug = runtimeSettings.SimulatorSlug,
            });

            string pathParams = $"/match";

            logger.Trace("MatchRoom - start", ("endpoint", roomsResolveEndpoint));

            requestFactory.SendRequest(roomsResolveEndpoint, pathParams, "POST", requestBody, null, $"{nameof(CloudRoomsService)}.{nameof(MatchRoom)}", authClient.SessionToken,
            response =>
            {
                var requestResponse = RequestResponse<RoomMatchResponse>.GetRequestResponse(response);

                if (requestResponse.Status == RequestStatus.Fail || response.Result == "")
                {
                    onRequestFinished?.Invoke(requestResponse);
                    return;
                }

                try
                {
                    var resp = Utils.CoherenceJson.DeserializeObject<RoomMatchResponse>(response.Result);
                    if (resp.Room != null)
                    {
                        resp.Room = DeserializeCreatedRoom(Utils.CoherenceJson.SerializeObject(resp.Room.Value));
                    }

                    requestResponse.Result = resp;
                }
                catch (Exception exception)
                {
                    requestResponse.Status = RequestStatus.Fail;
                    requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, exception.Message);

                    logger.Error(Error.RuntimeCloudDeserializationException,
                        ("Request", nameof(MatchRoom)),
                        ("Response", response.Result),
                        ("exception", exception));
                }
                finally
                {
                    onRequestFinished?.Invoke(requestResponse);
                }
            });
        }

        /// <summary>Match a room with the given options in the coherence Cloud asynchronously.</summary>
        /// <param name="tags">Filter the results by a list of tags.</param>
        public async Task<RoomMatchResponse> MatchRoomAsync(string[] tags = null)
        {
            var requestBody = Utils.CoherenceJson.SerializeObject(new RoomMatchRequest()
            {
                Tags = tags ?? new string[] { },
                Region = region,
                SimSlug = runtimeSettings.SimulatorSlug,
            });

            string pathParams = $"/match";

            logger.Trace("MatchRoomAsync - start", ("endpoint", roomsResolveEndpoint));

            var textResponse = await requestFactory.SendRequestAsync(roomsResolveEndpoint, pathParams, "POST", requestBody, null, $"{nameof(CloudRoomsService)}.{nameof(MatchRoomAsync)}", authClient.SessionToken);

            try
            {
                var resp = Utils.CoherenceJson.DeserializeObject<RoomMatchResponse>(textResponse);
                if (resp.Room != null)
                {
                    resp.Room = DeserializeCreatedRoom(Utils.CoherenceJson.SerializeObject(resp.Room.Value));
                }
                return resp;
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(MatchRoomAsync)),
                    ("Response", textResponse),
                    ("exception", exception));

                throw new ResponseDeserializationException(Result.InvalidResponse, exception.Message);
            }
        }

        /// <returns>Returns the internal cooldown for the Fetch Rooms endpoint.</returns>
        public TimeSpan GetFetchRoomsCooldown()
        {
            return requestFactory.GetRequestCooldown(roomsResolveEndpoint, "GET");
        }

        /// <summary>Fetch the available rooms in the coherence Cloud.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished, it includes the fetched rooms.</param>
        /// <param name="tags">Filter the results by a list of tags.</param>
        public void FetchRooms(Action<RequestResponse<IReadOnlyList<RoomData>>> onRequestFinished, string[] tags = null)
        {
            if (WaitForOngoingRequest(onRequestFinished))
            {
                return;
            }

            roomsCache.ClearRooms();

            var qs = new List<string>()
            {
                $"region={Uri.EscapeDataString(region)}"
            };

            if (tags != null && tags.Length > 0)
            {
                qs.Add($"tags={Uri.EscapeDataString(String.Join(",", tags ?? new string[] { }))}");
            }

            if (!string.IsNullOrEmpty(runtimeSettings.SimulatorSlug))
            {
                qs.Add($"sim_slug={Uri.EscapeDataString(runtimeSettings.SimulatorSlug)}");
            }

            string pathParams = $"?{String.Join("&", qs)}";

            logger.Trace("FetchRooms - start", ("endpoint", roomsResolveEndpoint), ("params", pathParams));

            requestFactory.SendRequest(roomsResolveEndpoint, pathParams, "GET", null, null, $"{nameof(CloudRoomsService)}.{nameof(FetchRooms)}", authClient.SessionToken,
                response =>
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
                        string authToken = GetAuthToken();

                        var rooms = OnFetch(response.Result, authToken); // Backend filters rooms by tags

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

        /// <summary>Fetch the available rooms in the coherence Cloud asynchronously.</summary>
        /// <param name="tags">Filter the results by a list of tags.</param>
        public async Task<IReadOnlyList<RoomData>> FetchRoomsAsync(string[] tags = null)
        {
            await AwaitForPreviousRequestAsync();

            roomsCache.ClearRooms();

            var qs = new List<string>(){
                $"tags={Uri.EscapeDataString(String.Join(",", tags ?? new string[] { }))}",
                $"region={Uri.EscapeDataString(region)}",
                $"sim_slug={Uri.EscapeDataString(runtimeSettings.SimulatorSlug)}"
            };

            string pathParams = $"?{String.Join("&", qs)}";

            logger.Trace("FetchRooms - start", ("endpoint", roomsResolveEndpoint));

            var responseText = await requestFactory.SendRequestAsync(roomsResolveEndpoint, pathParams, "GET", null, null, $"{nameof(CloudRoomsService)}.{nameof(FetchRoomsAsync)}", authClient.SessionToken);

            try
            {
                var authToken = GetAuthToken();

                var rooms = OnFetch(responseText, authToken); // Backend filters rooms by tags

                logger.Trace("FetchRooms - end", ("rooms count", rooms.Count));

                roomsCache.PopulateRooms(rooms);

                return rooms;
            }
            catch (Exception ex)
            {
                isFetchingRooms = false;

                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(FetchRoomsAsync)),
                    ("Response", responseText),
                    ("exception", ex));

                throw new ResponseDeserializationException(Result.InvalidResponse, ex.Message);
            }
            finally
            {
                isFetchingRooms = false;
            }
        }

        private async Task AwaitForPreviousRequestAsync()
        {
            while (isFetchingRooms)
            {
                await Task.Yield();
            }

            isFetchingRooms = true;
        }

        private RoomData DeserializeCreatedRoom(string textResponse)
        {
            var room = Utils.CoherenceJson.DeserializeObject<RoomData>(textResponse);
            if (runtimeSettings.IsWebGL)
            {
                room.Host.Ip = runtimeSettings.ApiEndpoint;
                room.Host.Port = runtimeSettings.RemoteWebPort;
            }

            room.AuthToken = authClient.SessionToken;

            logger.Trace("CreateRoom - end", ("roomID", room.Id));

            roomsCache.AddRoom(room);
            return room;
        }

        private bool WaitForOngoingRequest(Action<RequestResponse<IReadOnlyList<RoomData>>> onRequestFinished)
        {
            fetchRoomsCallbackList.Add(onRequestFinished);

            return fetchRoomsCallbackList.Count > 1;
        }

        private List<RoomData> OnFetch(string text, string sessionToken)
        {
            RoomFetchResponse response = Utils.CoherenceJson.DeserializeObject<RoomFetchResponse>(text);

            var result = response.Rooms.Select(room =>
            {
                if (runtimeSettings.IsWebGL)
                {
                    room.Host.Ip = runtimeSettings.ApiEndpoint;
                    room.Host.Port = runtimeSettings.RemoteWebPort;
                }

                room.AuthToken = sessionToken;

                return room;
            });

            return result.ToList();
        }

        private string GetRoomCreationRequestBody(RoomCreationOptions roomCreationOptions)
        {
            var requestBody = Utils.CoherenceJson.SerializeObject(new RoomCreationRequest()
            {
                Tags = roomCreationOptions.Tags ?? new string[] { },
                KV = roomCreationOptions.KeyValues ?? new Dictionary<string, string>(),
                Region = region,
                MaxClients = roomCreationOptions.MaxClients,
                SimSlug = runtimeSettings.SimulatorSlug,
                SimPayload = roomCreationOptions.SimPayload,
                FindOrCreate = roomCreationOptions.FindOrCreate,
            });
            return requestBody;
        }

        private SessionToken GetAuthToken()
        {
            var authToken = authClient.SessionToken;
#if UNITY
            authToken = !string.IsNullOrEmpty(SimulatorUtility.AuthToken)
                ? new(SimulatorUtility.AuthToken)
                : authToken;
#endif
            return authToken;
        }

        public void Dispose()
        {
            if (shouldDisposeRequestFactoryAndAuthClient)
            {
                shouldDisposeRequestFactoryAndAuthClient = false;
                CloudCredentialsPair.Dispose(authClient, requestFactory);
            }

            logger?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (shouldDisposeRequestFactoryAndAuthClient)
            {
                shouldDisposeRequestFactoryAndAuthClient = false;
                await CloudCredentialsPair.DisposeAsync(authClient, requestFactory);
            }

            logger?.Dispose();
        }
    }
}

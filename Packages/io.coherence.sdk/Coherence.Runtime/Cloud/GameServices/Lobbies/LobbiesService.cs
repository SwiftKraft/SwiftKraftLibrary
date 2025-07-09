// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Common;
    using Log;
    using Runtime;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logger = Log.Logger;

    public class LobbiesService : IAsyncDisposable, IDisposable
    {
        private IRequestFactory requestFactory;
        private IAuthClientInternal authClient;
        private readonly IRuntimeSettings runtimeSettings;
        private readonly Logger logger = Log.GetLogger<LobbiesService>();

        private static readonly string lobbiesResolveEndpoint = "/lobbies";
        private static readonly string playCallback = $"{lobbiesResolveEndpoint}/play";

        private Dictionary<string, LobbySession> lobbySessions = new();

        private List<Action<RequestResponse<IReadOnlyList<LobbyData>>>> fetchLobbiesCallbackList = new();
        private bool shouldDisposeRequestFactoryAndAuthClient;

        /// <summary>
        /// Callback that will be invoked when a Lobby owner you're a part of starts a game.
        /// The Callback contains the Lobby ID and the RoomData that you can use to join the game session through CoherenceBridge.JoinRoom.
        /// If a Callback is not supplied, coherence will automatically join the specified RoomData.
        /// </summary>
        public event Action<string, RoomData> OnPlaySessionStarted;

        internal event Action<RoomData> OnPlaySessionStartedInternal;

        public LobbiesService(CloudCredentialsPair credentialsPair = null, IRuntimeSettings runtimeSettings = null)
        {
#if UNITY
            runtimeSettings ??= RuntimeSettings.Instance;
#endif
            this.runtimeSettings = runtimeSettings;

            if (credentialsPair is null)
            {
                shouldDisposeRequestFactoryAndAuthClient = true;
                credentialsPair = CloudCredentialsFactory.ForClient(runtimeSettings);
                credentialsPair.authClient.LoginAsGuest().Then(task => logger.Warning(Warning.RuntimeCloudLoginFailedMsg, task.Exception.ToString()), TaskContinuationOptions.OnlyOnFaulted);
            }

            this.requestFactory = credentialsPair.RequestFactory;
            this.authClient = credentialsPair.authClient;

            this.authClient.OnLogout += OnLogout;

            requestFactory.AddPushCallback(playCallback, OnPlayStarted);
        }

        /// <returns>Returns the internal cooldown for the Find Or Create Lobby endpoint.</returns>
        public TimeSpan GetFindOrCreateLobbyCooldown()
        {
            return requestFactory.GetRequestCooldown(lobbiesResolveEndpoint + "/match", "POST");
        }

        /// <summary>Endpoint to do matchmaking and find a suitable Lobby. If no suitable Lobby is found, one will be created using the CreateLobbyOptions.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="findOptions">Options that will be used to try to find a suitable Lobby.</param>
        /// <param name="createOptions">Options that will be used to create a Lobby if no suitable Lobby is found.</param>
        public void FindOrCreateLobby(FindLobbyOptions findOptions, CreateLobbyOptions createOptions, Action<RequestResponse<LobbySession>> onRequestFinished)
        {
            AppendSimSlug(createOptions);

            var pathParams = "/match";

            var requestBodyForRegion = FindLobbyRequest.GetRequestBody(findOptions, createOptions);

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "POST", requestBodyForRegion,
                null, $"{nameof(LobbiesService)}.{nameof(FindOrCreateLobby)}", authClient.SessionToken, response =>
            {
                var requestResponse = RequestResponse<LobbySession>.GetRequestResponse(response);

                if (requestResponse.Status == RequestStatus.Fail)
                {
                    onRequestFinished?.Invoke(requestResponse);
                    return;
                }

                try
                {
                    LobbyData lobby = DeserializeLobbyData(response.Result);

                    logger.Trace("FindLobby - end", ("lobbyId", lobby.Id));

                    requestResponse.Result = CreateActiveLobbySession(lobby);
                }
                catch (Exception exception)
                {
                    requestResponse.Status = RequestStatus.Fail;
                    requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, exception.Message);

                    logger.Error(Error.RuntimeCloudDeserializationException,
                        ("Request", nameof(FindOrCreateLobby)),
                        ("Response", response.Result),
                        ("exception", exception));
                }
                finally
                {
                    onRequestFinished?.Invoke(requestResponse);
                }
            });
        }

        /// <summary>Endpoint to do matchmaking and find a suitable Lobby. If no suitable Lobby is found, one will be created using the CreateLobbyOptions.</summary>
        /// <param name="findOptions">Options that will be used to try to find a suitable Lobby.</param>
        /// <param name="createOptions">Options that will be used to create a Lobby if no suitable Lobby is found.</param>
        public async Task<LobbySession> FindOrCreateLobbyAsync(FindLobbyOptions findOptions, CreateLobbyOptions createOptions)
        {
            AppendSimSlug(createOptions);

            var pathParams = "/match";
            var requestBodyForRegion = FindLobbyRequest.GetRequestBody(findOptions, createOptions);

            var textResponse = await requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams,
                "POST", requestBodyForRegion, null, $"{nameof(LobbiesService)}.{nameof(FindOrCreateLobbyAsync)}", authClient.SessionToken);

            try
            {
                LobbyData lobby = DeserializeLobbyData(textResponse);

                logger.Trace("FindLobbyAsync - end", ("lobbyId", lobby.Id));

                return CreateActiveLobbySession(lobby);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(FindOrCreateLobbyAsync)),
                    ("Response", textResponse),
                    ("exception", exception));

                throw new ResponseDeserializationException(Result.InvalidResponse, exception.Message);
            }
        }

        /// <returns>Returns the internal cooldown for the Create Lobby endpoint.</returns>
        public TimeSpan GetCreateLobbyCooldown()
        {
            return requestFactory.GetRequestCooldown(lobbiesResolveEndpoint, "POST");
        }

        /// <summary>Endpoint to create a Lobby directly without doing matchmaking.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="createOptions">Options that will be used to create a Lobby.</param>
        public void CreateLobby(CreateLobbyOptions createOptions, Action<RequestResponse<LobbySession>> onRequestFinished)
        {
            AppendSimSlug(createOptions);

            var requestBodyForRegion = LobbyCreationRequest.GetRequestBody(createOptions);

            requestFactory.SendRequest(lobbiesResolveEndpoint, "POST", requestBodyForRegion,
                null, $"{nameof(LobbiesService)}.{nameof(CreateLobby)}", authClient.SessionToken, response =>
                {
                    var requestResponse = RequestResponse<LobbySession>.GetRequestResponse(response);

                    if (requestResponse.Status == RequestStatus.Fail)
                    {
                        onRequestFinished?.Invoke(requestResponse);
                        return;
                    }

                    try
                    {
                        LobbyData lobby = DeserializeLobbyData(response.Result);

                        logger.Trace("CreateLobby - end", ("lobbyId", lobby.Id));

                        requestResponse.Result = CreateActiveLobbySession(lobby);
                    }
                    catch (Exception exception)
                    {
                        requestResponse.Status = RequestStatus.Fail;
                        requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, exception.Message);

                        logger.Error(Error.RuntimeCloudDeserializationException,
                            ("Request", nameof(CreateLobby)),
                            ("Response", response.Result),
                            ("exception", exception));
                    }
                    finally
                    {
                        onRequestFinished?.Invoke(requestResponse);
                    }
                });
        }

        /// <summary>Endpoint to create a Lobby directly without doing matchmaking.</summary>
        /// <param name="createOptions">Options that will be used to create a Lobby.</param>
        public async Task<LobbySession> CreateLobbyAsync(CreateLobbyOptions createOptions)
        {
            AppendSimSlug(createOptions);

            var requestBodyForRegion = LobbyCreationRequest.GetRequestBody(createOptions);

            var textResponse = await requestFactory.SendRequestAsync(lobbiesResolveEndpoint,
                "POST", requestBodyForRegion, null, $"{nameof(LobbiesService)}.{nameof(CreateLobbyAsync)}", authClient.SessionToken);

            try
            {
                LobbyData lobby = DeserializeLobbyData(textResponse);

                logger.Trace("CreateLobbyAsync - end", ("lobbyId", lobby.Id));

                return CreateActiveLobbySession(lobby);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(CreateLobbyAsync)),
                    ("Response", textResponse),
                    ("exception", exception));

                throw new ResponseDeserializationException(Result.InvalidResponse, exception.Message);
            }
        }

        /// <returns>Returns the internal cooldown for the Find Lobbies endpoint.</returns>
        public TimeSpan GetFindLobbiesCooldown()
        {
            return requestFactory.GetRequestCooldown(lobbiesResolveEndpoint + "/search", "POST");
        }

        /// <summary>Find current active Lobbies that you will be able to join.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="findOptions">Optional parameter to filter the returned Lobbies.</param>
        public void FindLobbies(Action<RequestResponse<IReadOnlyList<LobbyData>>> onRequestFinished, FindLobbyOptions findOptions = null)
        {
            if (WaitForOngoingRequest(onRequestFinished))
            {
                return;
            }

            var pathParams = "/search";

            var requestBodyForRegion = FetchLobbiesRequest.GetRequestBody(findOptions);

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "POST", requestBodyForRegion,
                null, $"{nameof(LobbiesService)}.{nameof(FindLobbies)}", authClient.SessionToken, response =>
                {
                    var requestResponse = RequestResponse<IReadOnlyList<LobbyData>>.GetRequestResponse(response);

                    if (requestResponse.Status == RequestStatus.Fail)
                    {
                        requestResponse.Result = new List<LobbyData>();

                        foreach (var callback in fetchLobbiesCallbackList)
                        {
                            callback?.Invoke(requestResponse);
                        }

                        fetchLobbiesCallbackList.Clear();
                        return;
                    }

                    try
                    {
                        var lobbies = OnFetch(response.Result);

                        requestResponse.Result = lobbies;
                    }
                    catch (Exception exception)
                    {
                        requestResponse.Status = RequestStatus.Fail;
                        requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, exception.Message);

                        logger.Error(Error.RuntimeCloudDeserializationException,
                            ("Request", nameof(FindLobbies)),
                            ("Response", response.Result),
                            ("exception", exception));
                    }
                    finally
                    {
                        foreach (var callback in fetchLobbiesCallbackList)
                        {
                            callback?.Invoke(requestResponse);
                        }

                        fetchLobbiesCallbackList.Clear();
                    }
                });
        }

        /// <summary>Find current active Lobbies that you will be able to join.</summary>
        /// <param name="findOptions">Optional parameter to filter the returned Lobbies.</param>
        public async Task<IReadOnlyList<LobbyData>> FindLobbiesAsync(FindLobbyOptions findOptions = null)
        {
            var pathParams = "/search";

            var requestBodyForRegion = FetchLobbiesRequest.GetRequestBody(findOptions);

            var textResponse = await requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams,
                "POST", requestBodyForRegion, null, $"{nameof(LobbiesService)}.{nameof(FindLobbiesAsync)}", authClient.SessionToken);

            try
            {
                var lobbies = OnFetch(textResponse);

                logger.Trace("FindLobbiesAsync - end", ("lobbies count", lobbies.Count));

                return lobbies;
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(FindLobbiesAsync)),
                    ("Response", textResponse),
                    ("exception", exception));

                throw new ResponseDeserializationException(Result.InvalidResponse, exception.Message);
            }
        }

        /// <summary>Join the supplied Lobby.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="playerAttr">Optional parameter to add player attributes to the joined Lobby.</param>
        /// <param name="secret">Optional parameter to specify the Secret to join a private Lobby.</param>
        public void JoinLobby(LobbyData lobby, Action<RequestResponse<LobbySession>> onRequestFinished,
            List<CloudAttribute> playerAttr = null, string secret = null)
        {
            var pathParams = $"/{lobby.Id}/players";

            var requestBody = JoinLobbyRequest.GetRequestBody(playerAttr, secret);

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "POST", requestBody,
                null, $"{nameof(LobbiesService)}.{nameof(JoinLobby)}", authClient.SessionToken, response =>
                {
                    var requestResponse = RequestResponse<LobbySession>.GetRequestResponse(response);

                    if (requestResponse.Status == RequestStatus.Fail)
                    {
                        onRequestFinished?.Invoke(requestResponse);
                        return;
                    }

                    try
                    {
                        LobbyData joinedLobby = DeserializeLobbyData(response.Result);

                        logger.Trace("JoinLobby - end", ("lobbyId", joinedLobby.Id));

                        requestResponse.Result = CreateActiveLobbySession(joinedLobby);
                    }
                    catch (Exception exception)
                    {
                        requestResponse.Status = RequestStatus.Fail;
                        requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, exception.Message);

                        logger.Error(Error.RuntimeCloudDeserializationException,
                            ("Request", nameof(JoinLobby)),
                            ("Response", response.Result),
                            ("exception", exception));
                    }
                    finally
                    {
                        onRequestFinished?.Invoke(requestResponse);
                    }
                });
        }

        /// <summary>Join the supplied Lobby.</summary>
        /// <param name="playerAttr">Optional parameter to add player attributes to the joined Lobby.</param>
        /// <param name="secret">Optional parameter to specify the Secret to join a private Lobby.</param>
        public async Task<LobbySession> JoinLobbyAsync(LobbyData lobby, List<CloudAttribute> playerAttr = null, string secret = null)
        {
            var pathParams = $"/{lobby.Id}/players";

            var requestBody = JoinLobbyRequest.GetRequestBody(playerAttr, secret);

            var textResponse = await requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams,
                "POST", requestBody, null, $"{nameof(LobbiesService)}.{nameof(JoinLobbyAsync)}", authClient.SessionToken);

            try
            {
                LobbyData joinedLobby = DeserializeLobbyData(textResponse);

                logger.Trace("JoinLobbyAsync - end", ("lobbyId", joinedLobby.Id));

                return CreateActiveLobbySession(joinedLobby);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(JoinLobbyAsync)),
                    ("Response", textResponse),
                    ("exception", exception));

                throw new ResponseDeserializationException(Result.InvalidResponse, exception.Message);
            }
        }

        /// <summary>Refresh the current data for the supplied Lobby.</summary>
        /// <param name="lobby">Lobby you want to refresh the data of.</param>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        public void RefreshLobby(LobbyData lobby, Action<RequestResponse<LobbyData>> onRequestFinished)
        {
            RefreshLobby(lobby.Id, onRequestFinished);
        }

        /// <summary>Refresh the current data for the supplied Lobby.</summary>
        /// <param name="lobby">Lobby you want to refresh the data of.</param>
        public async Task<LobbyData> RefreshLobbyAsync(LobbyData lobby)
        {
            return await RefreshLobbyAsync(lobby.Id);
        }

        /// <summary>Refresh the current data for the Lobby with the supplied id.</summary>
        /// <param name="lobbyId">Lobby ID you want to refresh the data of.</param>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        public void RefreshLobby(string lobbyId, Action<RequestResponse<LobbyData>> onRequestFinished)
        {
            var pathParams = $"/{lobbyId}";

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "GET", null,
                null, $"{nameof(LobbiesService)}.{nameof(RefreshLobby)}", authClient.SessionToken, response =>
                {
                    var requestResponse = RequestResponse<LobbyData>.GetRequestResponse(response);

                    if (requestResponse.Status == RequestStatus.Fail)
                    {
                        onRequestFinished?.Invoke(requestResponse);
                        return;
                    }

                    try
                    {
                        LobbyData updatedLobby = DeserializeLobbyData(response.Result);

                        logger.Trace("RefreshLobby - end", ("lobbyId", updatedLobby.Id));

                        requestResponse.Result = updatedLobby;

                        CreateActiveLobbySessionIfPlayerIsInLobby(updatedLobby);
                    }
                    catch (Exception exception)
                    {
                        requestResponse.Status = RequestStatus.Fail;
                        requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, exception.Message);

                        logger.Error(Error.RuntimeCloudDeserializationException,
                            ("Request", nameof(RefreshLobby)),
                            ("Response", response.Result),
                            ("exception", exception));
                    }
                    finally
                    {
                        onRequestFinished?.Invoke(requestResponse);
                    }
                });
        }

        /// <summary>Refresh the current data for the Lobby with the supplied id.</summary>
        /// <param name="lobbyId">Lobby ID you want to refresh the data of.</param>
        public async Task<LobbyData> RefreshLobbyAsync(string lobbyId)
        {
            var pathParams = $"/{lobbyId}";

            var textResponse = await requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams,
                "GET", null, null, $"{nameof(LobbiesService)}.{nameof(RefreshLobbyAsync)}", authClient.SessionToken);

            try
            {
                LobbyData updatedLobby = DeserializeLobbyData(textResponse);

                logger.Trace("RefreshLobbyAsync - end", ("lobbyId", updatedLobby.Id));

                CreateActiveLobbySessionIfPlayerIsInLobby(updatedLobby);

                return updatedLobby;
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(RefreshLobbyAsync)),
                    ("Response", textResponse),
                    ("exception", exception));

                throw new ResponseDeserializationException(Result.InvalidResponse, exception.Message);
            }
        }

        /// <summary>Get stats for the usage of Lobbies for your current coherence Project.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="tags">Optional list of tags to filter the fetched stats.</param>
        /// <param name="regions">Optional list of regions to filter the fetched stats.</param>
        public void FetchLobbyStats(Action<RequestResponse<LobbyStats>> onRequestFinished, List<string> tags = null, List<string> regions = null)
        {
            var pathParams = "/stats";

            var requestBody = StatsRequest.GetRequestBody(tags, regions);

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "POST", requestBody,
                null, $"{nameof(LobbiesService)}.{nameof(FetchLobbyStats)}", authClient.SessionToken, response =>
                {
                    var requestResponse = RequestResponse<LobbyStats>.GetRequestResponse(response);

                    if (requestResponse.Status == RequestStatus.Fail)
                    {
                        onRequestFinished?.Invoke(requestResponse);
                        return;
                    }

                    try
                    {
                        var stats = Utils.CoherenceJson.DeserializeObject<LobbyStats>(response.Result);

                        requestResponse.Result = stats;
                    }
                    catch (Exception exception)
                    {
                        requestResponse.Status = RequestStatus.Fail;
                        requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, exception.Message);

                        logger.Error(Error.RuntimeCloudDeserializationException,
                            ("Request", nameof(FetchLobbyStats)),
                            ("Response", response.Result),
                            ("exception", exception));
                    }
                    finally
                    {
                        onRequestFinished?.Invoke(requestResponse);
                    }
                });
        }

        /// <summary>Get stats for the usage of Lobbies for your current coherence Project.</summary>
        /// <param name="tags">Optional list of tags to filter the fetched stats.</param>
        /// <param name="regions">Optional list of regions to filter the fetched stats.</param>
        public async Task<LobbyStats> FetchLobbyStatsAsync(List<string> tags = null, List<string> regions = null)
        {
            var pathParams = "/stats";

            var requestBody = StatsRequest.GetRequestBody(tags, regions);

            var textResponse = await requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams,
                "POST", requestBody, null, $"{nameof(LobbiesService)}.{nameof(FetchLobbyStats)}", authClient.SessionToken);

            try
            {
                var stats = Utils.CoherenceJson.DeserializeObject<LobbyStats>(textResponse);

                return stats;
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(FetchLobbyStats)),
                    ("Response", textResponse),
                    ("exception", exception));

                throw new ResponseDeserializationException(Result.InvalidResponse, exception.Message);
            }
        }

        /// <summary>Get a LobbySession instance for a Lobby that you have joined and you're a part of.</summary>
        public async Task<LobbySession> GetActiveLobbySessionForLobbyId(string lobbyId)
        {
            if (lobbySessions.TryGetValue(lobbyId, out var lobbySession) && !lobbySession.IsDisposed)
            {
                return lobbySession;
            }

            var lobbyData = await RefreshLobbyAsync(lobbyId);

            CreateActiveLobbySessionIfPlayerIsInLobby(lobbyData);

            lobbySessions.TryGetValue(lobbyId, out lobbySession);

            return lobbySession;
        }

        /// <summary>Iterate all active LobbySession instances.</summary>
        /// <remarks>A LobbySession instance is used to interface with a Lobby that you are a part of.</remarks>
        public IEnumerable<LobbySession> GetLobbySessions()
        {
            var disposedSessions = new List<LobbySession>();

            foreach (var lobbySession in lobbySessions.Values)
            {
                if (!lobbySession.IsDisposed)
                {
                    yield return lobbySession;
                }
                else
                {
                    disposedSessions.Add(lobbySession);
                }
            }

            foreach (var disposedSession in disposedSessions)
            {
                lobbySessions.Remove(disposedSession.LobbyData.Id);
            }
        }

        public void Dispose()
        {
            foreach (var lobbySession in lobbySessions)
            {
                lobbySession.Value.Dispose();
            }

            lobbySessions.Clear();

            if (shouldDisposeRequestFactoryAndAuthClient)
            {
                shouldDisposeRequestFactoryAndAuthClient = false;
                CloudCredentialsPair.Dispose(authClient, requestFactory);
            }

            logger?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var lobbySession in lobbySessions)
            {
                lobbySession.Value.Dispose();
            }

            lobbySessions.Clear();

            if (shouldDisposeRequestFactoryAndAuthClient)
            {
                shouldDisposeRequestFactoryAndAuthClient = false;
                await CloudCredentialsPair.DisposeAsync(authClient, requestFactory);
            }

            logger?.Dispose();
        }

        private LobbyData DeserializeLobbyData(string response)
        {
            var lobby = Utils.CoherenceJson.DeserializeObject<LobbyData>(response);

            if (!lobby.RoomData.HasValue)
            {
                return lobby;
            }

            var room = lobby.RoomData.Value;
            AddTokenToRoom(ref room);
            lobby.RoomData = room;

            return lobby;
        }

        private void AppendSimSlug(CreateLobbyOptions options)
        {
            if (string.IsNullOrEmpty(options.SimulatorSlug) && !string.IsNullOrEmpty(runtimeSettings.SimulatorSlug))
            {
                options.SimulatorSlug = runtimeSettings.SimulatorSlug;
            }
        }

        private List<LobbyData> OnFetch(string text)
        {
            List<LobbyData> response = Utils.CoherenceJson.DeserializeObject<List<LobbyData>>(text);

            return response;
        }

        private LobbySession CreateActiveLobbySession(LobbyData lobby)
        {
            var lobbySession = new LobbySession(this, lobby, authClient, requestFactory);

            lobbySessions[lobby.Id] = lobbySession;

            return lobbySession;
        }

        private void CreateActiveLobbySessionIfPlayerIsInLobby(LobbyData updatedLobby)
        {
            if (lobbySessions.TryGetValue(updatedLobby.Id, out var lobbySession) && lobbySession.IsDisposed)
            {
                if (lobbySession.IsDisposed)
                {
                    lobbySessions.Remove(updatedLobby.Id);
                }
                else
                {
                    return;
                }
            }

            foreach (var player in updatedLobby.Players)
            {
                if (player.Id.Equals(authClient.PlayerAccountId))
                {
                    CreateActiveLobbySession(updatedLobby);
                }
            }
        }

        private bool WaitForOngoingRequest(Action<RequestResponse<IReadOnlyList<LobbyData>>> onRequestFinished)
        {
            fetchLobbiesCallbackList.Add(onRequestFinished);

            return fetchLobbiesCallbackList.Count > 1;
        }

        private void OnPlayStarted(string responseBody)
        {
            PlayCallbackResponse response = default;

            try
            {
                response = Utils.CoherenceJson.DeserializeObject<PlayCallbackResponse>(responseBody);

                AddTokenToRoom(ref response.Room);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(ConnectionClosedResponse)),
                    ("Response", responseBody),
                    ("exception", exception));

                return;
            }

            if (OnPlaySessionStarted != null)
            {
                OnPlaySessionStarted.Invoke(response.LobbyId, response.Room);
            }
            else
            {
                OnPlaySessionStartedInternal?.Invoke(response.Room);
            }
        }

        private void AddTokenToRoom(ref RoomData room)
        {
            if (runtimeSettings.IsWebGL)
            {
                room.Host.Ip = runtimeSettings.ApiEndpoint;
                room.Host.Port = runtimeSettings.RemoteWebPort;
            }

            room.AuthToken = authClient.SessionToken;
        }

        private void OnLogout()
        {
            lobbySessions.Clear();
        }
    }
}

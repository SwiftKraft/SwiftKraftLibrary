// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Runtime;
    using System;
    using System.Threading.Tasks;
    using Log;
    using System.Collections.Generic;
    using Common;
    using Logger = Log.Logger;

    public class LobbySession : IDisposableInternal
    {
        private LobbiesService lobbiesService;
        private LobbyData lobbyData;
        private IAuthClientInternal authClient;
        private IRequestFactory requestFactory;

        private LobbyOwnerSession lobbyOwnerSession;
        private LobbyPlayer? myPlayer;
        private LobbyPlayer ownerPlayer;

        private static readonly string lobbiesResolveEndpoint = "/lobbies";
        private static readonly string messageReceivedCallback = $"{lobbiesResolveEndpoint}/messages";
        private static readonly string playerJoinedCallback = $"{lobbiesResolveEndpoint}/player/join";
        private static readonly string playerLeftCallback = $"{lobbiesResolveEndpoint}/player/leave";
        private static readonly string playerAttributesChanged = $"{lobbiesResolveEndpoint}/players/attrs";
        private static readonly string lobbyOwnerChanged = $"{lobbiesResolveEndpoint}/owner";
        private static readonly string lobbyAttributesChanged = $"{lobbiesResolveEndpoint}/attrs";

        private readonly Logger logger = Log.GetLogger<LobbySession>();

        public LobbyData LobbyData => lobbyData;

        /// <summary>
        /// Callback that will be invoked when your Player receives one or messages through the Lobby.
        /// </summary>
        public event Action<LobbySession, MessagesReceived> OnMessageReceived;

        /// <summary>
        /// Callback that will be invoked when a new Player joins the Lobby.
        /// </summary>
        public event Action<LobbySession, LobbyPlayer> OnPlayerJoined;

        /// <summary>
        /// Callback that will be invoked when a Player leaves the Lobby.
        /// </summary>
        public event Action<LobbySession, LobbyPlayer, string> OnPlayerLeft;

        /// <summary>
        /// Callback that will be invoked when a Player within the Lobby adds or updates an Attribute.
        /// </summary>
        public event Action<LobbySession, LobbyPlayer, IReadOnlyList<CloudAttribute>> OnPlayerAttributesChanged;

        /// <summary>
        /// Callback that will be invoked when the Lobby changes active owner.
        /// </summary>
        public event Action<LobbySession, LobbyPlayer> OnLobbyOwnerChanged;

        /// <summary>
        /// Callback that will be invoked when the owner of the Lobby adds or updates a Lobby attribute,
        /// or when an indexed Attribute gets updated through a Player updating his/her own Attribute.
        /// </summary>
        public event Action<LobbySession, IReadOnlyList<CloudAttribute>> OnLobbyAttributesChanged;

        /// <summary>
        /// Callback that will be invoked when a Lobby its updated by any of the previous cases.
        /// For example, if a player leaves or joins or updates the Attributes, this Callback will always be invoked
        /// alongside the specific one.
        /// </summary>
        public event Action<LobbySession> OnLobbyUpdated;

        /// <summary>
        /// Event that is raised when a Lobby is disposed and will no longer be usable.
        /// This can happen when <see cref="LeaveLobby">leaving the Lobby</see> and
        /// when <see cref="PlayerAccount.Logout">logging out</see> from coherence Cloud.
        /// </summary>
        public event Action<LobbySession> OnLobbyDisposed;

        /// <summary>
        /// Player struct that represents your Player within the Lobby.
        /// </summary>
        public LobbyPlayer? MyPlayer => myPlayer;

        /// <summary>
        /// Player struct that represents the Player who is the Owner of the Lobby.
        /// </summary>
        public LobbyPlayer OwnerPlayer => ownerPlayer;

        /// <summary>
        /// Instance of LobbyOwnerSession that will allow you to call endpoints that are only available for the Owner of the Lobby.
        /// If you're not the Owner of the Lobby, this property will return null.
        /// </summary>
        public LobbyOwnerSession LobbyOwnerActions => lobbyOwnerSession;

        /// <summary>
        /// If the current LobbySession is disposed, it means that you're no longer part of this Lobby and this instance cannot be reused.
        /// Attempting to use any Endpoint while disposed will throw an exception.
        /// </summary>
        public bool IsDisposed { get; private set; }

        string IDisposableInternal.InitializationContext { get; set; }
        string IDisposableInternal.InitializationStackTrace { get; set; }
        bool IDisposableInternal.IsDisposed
        {
            get => IsDisposed;
            set => IsDisposed = value;
        }

        public LobbySession(LobbiesService lobbiesService, LobbyData lobbyData, AuthClient authClient, RequestFactory requestFactory)
            : this(lobbiesService, lobbyData, (IAuthClientInternal)authClient, requestFactory) { }

        internal LobbySession(LobbiesService lobbiesService, LobbyData lobbyData, IAuthClientInternal authClient, IRequestFactory requestFactory)
        {
            this.OnInitialized();
            this.lobbiesService = lobbiesService;
            this.lobbyData = lobbyData;
            this.authClient = authClient;
            this.requestFactory = requestFactory;

            ParseLobbyData();
            requestFactory.AddPushCallback(messageReceivedCallback, OnMessageReceivedHandler);
            requestFactory.AddPushCallback(playerJoinedCallback, OnPlayerJoinedHandler);
            requestFactory.AddPushCallback(playerLeftCallback, OnPlayerLeftHandler);
            requestFactory.AddPushCallback(playerAttributesChanged, OnPlayerAttributesChangedHandler);
            requestFactory.AddPushCallback(lobbyOwnerChanged, OnLobbyOwnerChangedHandler);
            requestFactory.AddPushCallback(lobbyAttributesChanged, OnLobbyAttributesChangedHandler);

            this.authClient.OnLogout += OnLogout;
        }

        /// <summary>Refresh the current data for the active Lobby.</summary>
        public void RefreshLobby(Action onFinished)
        {
            lobbiesService.RefreshLobby(lobbyData, response =>
            {
                if (response.Status == RequestStatus.Success)
                {
                    lobbyData = response.Result;
                    ParseLobbyData();
                }

                onFinished?.Invoke();
            });
        }

        /// <summary>Refresh the current data for the active Lobby.</summary>
        public async Task RefreshLobbyAsync()
        {
            lobbyData = await lobbiesService.RefreshLobbyAsync(lobbyData);

            ParseLobbyData();
        }

        /// <summary>Leave the active Lobby. The current LobbySession instance will be disposed.</summary>
        public void LeaveLobby(Action<RequestResponse<bool>> onRequestFinished)
        {
            ThrowIfDisposed();

            var pathParams = $"/{lobbyData.Id}/players/{authClient.PlayerAccountId}";

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "DELETE", null,
                null, $"{nameof(LobbySession)}.{nameof(LeaveLobby)}", authClient.SessionToken, response =>
                {
                    var requestResponse = RequestResponse<bool>.GetRequestResponse(response);

                    if (response.Status == RequestStatus.Success
                        || (response.Exception is RequestException requestException && requestException.ErrorCode == ErrorCode.LobbyActionNotAllowed))
                    {
                        requestResponse.Status = RequestStatus.Success;
                        requestResponse.Exception = null;
                        requestResponse.Result = true;

                        Dispose();

                        onRequestFinished?.Invoke(requestResponse);
                    }
                    else
                    {
                        requestResponse.Result = false;
                        onRequestFinished?.Invoke(requestResponse);
                    }
                });
        }

        /// <summary>Leave the active Lobby. The current LobbySession instance will be disposed.</summary>
        public async Task<bool> LeaveLobbyAsync()
        {
            ThrowIfDisposed();

            var pathParams = $"/{lobbyData.Id}/players/{authClient.PlayerAccountId}";

            var task = requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams, "DELETE", null, null, $"{nameof(LobbySession)}.{nameof(LeaveLobbyAsync)}", authClient.SessionToken);

            await task;

            if (task.Exception != null &&
                (!(task.Exception.InnerException is RequestException requestException)
                 || requestException.ErrorCode != ErrorCode.LobbyActionNotAllowed))
            {
                throw task.Exception;
            }

            Dispose();

            return true;
        }

        /// <returns>Returns the internal cooldown for the Send Message endpoint.</returns>
        public TimeSpan GetSendMessageCooldown()
        {
            return requestFactory.GetRequestCooldown(lobbiesResolveEndpoint + $"/{lobbyData.Id}/messages", "POST");
        }

        /// <summary>Send a message to other Players of the current Lobby.</summary>
        /// <param name="messages">List of messages to be sent to other Players.</param>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="targets">Optional list of Players to send the messages to. If left empty, it will be send to everyone.</param>
        public void SendMessage(List<string> messages, Action<RequestResponse<bool>> onRequestFinished, List<LobbyPlayer> targets = null)
        {
            ThrowIfDisposed();

            var pathParams = $"/{lobbyData.Id}/messages";

            var requestBody = SendMessageRequest.GetRequestBody(messages, targets);

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "POST", requestBody,
                null, $"{nameof(LobbySession)}.{nameof(SendMessage)}", authClient.SessionToken, response =>
                {
                    var requestResponse = RequestResponse<bool>.GetRequestResponse(response);

                    if (response.Status == RequestStatus.Success)
                    {
                        requestResponse.Result = true;
                        onRequestFinished?.Invoke(requestResponse);
                    }
                    else
                    {
                        requestResponse.Result = false;
                        onRequestFinished?.Invoke(requestResponse);
                    }
                });
        }

        /// <summary>Send a message to other Players of the current Lobby.</summary>
        /// <param name="messages">List of messages to be sent to other Players.</param>
        /// <param name="targets">Optional list of Players to send the messages to. If left empty, it will be send to everyone.</param>
        public async Task<bool> SendMessageAsync(List<string> messages, List<LobbyPlayer> targets = null)
        {
            ThrowIfDisposed();

            var pathParams = $"/{lobbyData.Id}/messages";

            var requestBody = SendMessageRequest.GetRequestBody(messages, targets);

            var task = requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams,
                "POST", requestBody, null, $"{nameof(LobbySession)}.{nameof(SendMessageAsync)}", authClient.SessionToken);

            await task;

            if (task.Exception != null)
            {
                throw task.Exception;
            }

            return true;
        }

        /// <returns>Returns the internal cooldown for the Add Or Update My Attributes endpoint.</returns>
        public TimeSpan GetAddOrUpdateMyAttributesCooldown()
        {
            return requestFactory.GetRequestCooldown(lobbiesResolveEndpoint + $"/{lobbyData.Id}/players/{authClient.PlayerAccountId}/attrs", "PATCH");
        }

        /// <summary>
        /// Through this method you will be able to add new Attributes or update existing Attributes.
        /// Deleting existing Attributes is not supported, the list supplied as parameter will be merged with your
        /// current Attributes if the request succeeds with the backend.
        /// </summary>
        /// <param name="attributes">List of Attributes to be added or updated for the current Lobby.</param>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        public void AddOrUpdateMyAttributes(List<CloudAttribute> attributes, Action<RequestResponse<bool>> onRequestFinished)
        {
            ThrowIfDisposed();

            var pathParams = $"/{lobbyData.Id}/players/{authClient.PlayerAccountId}/attrs";

            var requestBody = SetAttributesRequest.GetRequestBody(attributes);

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "PATCH", requestBody,
                null, $"{nameof(LobbySession)}.{nameof(AddOrUpdateMyAttributes)}", authClient.SessionToken, response =>
                {
                    var requestResponse = RequestResponse<bool>.GetRequestResponse(response);

                    if (response.Status == RequestStatus.Success)
                    {
                        requestResponse.Result = true;
                        myPlayer.Value.MergeAttributesWith(attributes);
                        onRequestFinished?.Invoke(requestResponse);
                    }
                    else
                    {
                        requestResponse.Result = false;
                        onRequestFinished?.Invoke(requestResponse);
                    }
                });
        }

        /// <summary>
        /// Through this method you will be able to add new Attributes or update existing Attributes.
        /// Deleting existing Attributes is not supported, the list supplied as parameter will be merged with your
        /// current Attributes if the request succeeds with the backend.
        /// </summary>
        /// <param name="attributes">List of Attributes to be added or updated for the current Lobby.</param>
        public async Task<bool> AddOrUpdateMyAttributesAsync(List<CloudAttribute> attributes)
        {
            ThrowIfDisposed();

            var pathParams = $"/{lobbyData.Id}/players/{authClient.PlayerAccountId}/attrs";

            var requestBody = SetAttributesRequest.GetRequestBody(attributes);

            var task = requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams, "PATCH",
                requestBody, null, $"{nameof(LobbySession)}.{nameof(AddOrUpdateMyAttributesAsync)}", authClient.SessionToken);

            await task;

            if (task.Exception != null)
            {
                throw task.Exception;
            }

            myPlayer.Value.MergeAttributesWith(attributes);

            return true;
        }

        ~LobbySession()
        {
            if (!this.OnFinalized())
            {
                logger.Warning(Warning.RuntimeCloudGameServicesResourceLeak, this.GetResourceLeakWarningMessage());
            }
        }

        public void Dispose()
        {
            if (this.OnDisposed())
            {
                return;
            }

            requestFactory.RemovePushCallback(messageReceivedCallback, OnMessageReceivedHandler);
            requestFactory.RemovePushCallback(playerJoinedCallback, OnPlayerJoinedHandler);
            requestFactory.RemovePushCallback(playerLeftCallback, OnPlayerLeftHandler);
            requestFactory.RemovePushCallback(playerAttributesChanged, OnPlayerAttributesChangedHandler);
            requestFactory.RemovePushCallback(lobbyOwnerChanged, OnLobbyOwnerChangedHandler);
            requestFactory.RemovePushCallback(lobbyAttributesChanged, OnLobbyAttributesChangedHandler);
            this.authClient.OnLogout -= OnLogout;
            logger.Dispose();
            OnLobbyDisposed?.Invoke(this);
        }

        private void OnLogout() => Dispose();

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new RequestException(0,
                    $"{nameof(LobbySession)} has been disposed. This session is no longer usable.");
            }
        }

        private void ParseLobbyData()
        {
            myPlayer = null;
            lobbyOwnerSession = null;

            foreach (var player in lobbyData.Players)
            {
                if (player.Id.Equals(authClient.PlayerAccountId))
                {
                    myPlayer = player;
                }

                if (lobbyData.OwnerId == player.Id)
                {
                    ownerPlayer = player;
                }
            }

            UpdateLobbyOwnerSession();

            if (!myPlayer.HasValue)
            {
                Dispose();
            }
        }

        private void UpdateLobbyOwnerSession()
        {
            lobbyOwnerSession = myPlayer.HasValue && myPlayer.Value == ownerPlayer
                ? new LobbyOwnerSession(this.LobbyData, this.authClient, this.requestFactory)
                : null;
        }

        private void OnMessageReceivedHandler(string responseBody)
        {
            MessagesReceived response = default;
            try
            {
                response = Utils.CoherenceJson.DeserializeObject<MessagesReceived>(responseBody);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(MessagesReceived)),
                    ("Response", responseBody),
                    ("exception", exception));

                return;
            }

            if (response.LobbyId.Equals(lobbyData.Id))
            {
                OnMessageReceived?.Invoke(this, response);
            }
        }

        private void OnPlayerJoinedHandler(string responseBody)
        {
            PlayerJoinedPayload response = default;

            try
            {
                response = Utils.CoherenceJson.DeserializeObject<PlayerJoinedPayload>(responseBody);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(PlayerJoinedPayload)),
                    ("Response", responseBody),
                    ("exception", exception));

                return;
            }

            if (!response.LobbyId.Equals(lobbyData.Id))
            {
                return;
            }

            if (!lobbyData.players.Contains(response.Player))
            {
                lobbyData.players.Add(response.Player);
            }

            ParseLobbyData();

            OnPlayerJoined?.Invoke(this, response.Player);
            OnLobbyUpdated?.Invoke(this);
        }

        private void OnPlayerLeftHandler(string responseBody)
        {
            PlayerLeftPayload response = default;

            try
            {
                response = Utils.CoherenceJson.DeserializeObject<PlayerLeftPayload>(responseBody);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(PlayerLeftPayload)),
                    ("Response", responseBody),
                    ("exception", exception));

                return;
            }

            if (!response.LobbyId.Equals(lobbyData.Id))
            {
                return;
            }

            LobbyPlayer? playerToBeRemoved = null;

            foreach (var player in lobbyData.Players)
            {
                if (player.Id.Equals(response.PlayerId))
                {
                    playerToBeRemoved = player;
                    break;
                }
            }

            if (!playerToBeRemoved.HasValue)
            {
                return;
            }

            lobbyData.players.Remove(playerToBeRemoved.Value);
            ParseLobbyData();

            OnPlayerLeft?.Invoke(this, playerToBeRemoved.Value, response.Reason);
            OnLobbyUpdated?.Invoke(this);
        }

        private void OnPlayerAttributesChangedHandler(string responseBody)
        {
            PlayerAttributesChangedPayload response = default;

            try
            {
                response = Utils.CoherenceJson.DeserializeObject<PlayerAttributesChangedPayload>(responseBody);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(PlayerAttributesChangedPayload)),
                    ("Response", responseBody),
                    ("exception", exception));

                return;
            }

            if (!response.LobbyId.Equals(lobbyData.Id))
            {
                return;
            }

            LobbyPlayer? playerWithChangedAttributes = null;

            foreach (var player in lobbyData.Players)
            {
                if (player.Id.Equals(response.PlayerId))
                {
                    playerWithChangedAttributes = player;
                    player.MergeAttributesWith(response.AttributesChanged);
                    break;
                }
            }

            if (!playerWithChangedAttributes.HasValue)
            {
                return;
            }

            OnPlayerAttributesChanged?.Invoke(this, playerWithChangedAttributes.Value, playerWithChangedAttributes.Value.Attributes);
            OnLobbyUpdated?.Invoke(this);
        }

        private void OnLobbyOwnerChangedHandler(string responseBody)
        {
            OwnerChangedPayload response = default;
            try
            {
                response = Utils.CoherenceJson.DeserializeObject<OwnerChangedPayload>(responseBody);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(OwnerChangedPayload)),
                    ("Response", responseBody),
                    ("exception", exception));

                return;
            }

            if (!response.LobbyId.Equals(lobbyData.Id))
            {
                return;
            }

            LobbyPlayer? newOwner = null;

            foreach (var player in lobbyData.Players)
            {
                if (player.Id.Equals(response.PlayerId))
                {
                    newOwner = player;
                    break;
                }
            }

            if (!newOwner.HasValue)
            {
                return;
            }

            lobbyData.ownerId = newOwner.Value.Id;
            ownerPlayer = newOwner.Value;

            UpdateLobbyOwnerSession();

            OnLobbyOwnerChanged?.Invoke(this, newOwner.Value);
            OnLobbyUpdated?.Invoke(this);
        }

        private void OnLobbyAttributesChangedHandler(string responseBody)
        {
            LobbyAttributesChangedPayload response = default;

            try
            {
                response = Utils.CoherenceJson.DeserializeObject<LobbyAttributesChangedPayload>(responseBody);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(LobbyAttributesChangedPayload)),
                    ("Response", responseBody),
                    ("exception", exception));

                return;
            }

            if (!response.LobbyId.Equals(lobbyData.Id))
            {
                return;
            }

            lobbyData.MergeAttributesWith(response.AttributesChanged);

            OnLobbyAttributesChanged?.Invoke(this, lobbyData.lobbyAttributes);
            OnLobbyUpdated?.Invoke(this);
        }
    }
}

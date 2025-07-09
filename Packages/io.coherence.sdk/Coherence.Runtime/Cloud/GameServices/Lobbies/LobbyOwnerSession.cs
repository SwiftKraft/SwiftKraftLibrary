// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class LobbyOwnerSession
    {
        private LobbyData lobbyData;
        private IAuthClientInternal authClient;
        private IRequestFactory requestFactory;

        private static readonly string lobbiesResolveEndpoint = "/lobbies";

        public LobbyOwnerSession(LobbyData lobbyData, AuthClient authClient, RequestFactory requestFactory)
            : this(lobbyData, (IAuthClientInternal)authClient, requestFactory) { }

        internal LobbyOwnerSession(LobbyData lobbyData, IAuthClientInternal authClient, IRequestFactory requestFactory)
        {
            this.lobbyData = lobbyData;
            this.authClient = authClient;
            this.requestFactory = requestFactory;
        }

        /// <summary>Kick a Player from the active Lobby.</summary>
        /// <param name="player">Player that will be kicked from the Lobby.</param>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        public void KickPlayer(LobbyPlayer player, Action<RequestResponse<bool>> onRequestFinished)
        {
            var pathParams = $"/{lobbyData.Id}/players/{player.Id}";

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "DELETE", null,
                null, $"{nameof(LobbyOwnerSession)}.{nameof(KickPlayer)}", authClient.SessionToken, response =>
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

        /// <summary>Kick a Player from the active Lobby.</summary>
        /// <param name="player">Player that will be kicked from the Lobby.</param>
        public async Task<bool> KickPlayerAsync(LobbyPlayer player)
        {
            var pathParams = $"/{lobbyData.Id}/players/{player.Id}";

            var task = requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams, "DELETE",
                null, null, $"{nameof(LobbyOwnerSession)}.{nameof(KickPlayerAsync)}", authClient.SessionToken);

            await task;

            if (task.Exception != null)
            {
                throw task.Exception;
            }

            return true;
        }

        /// <returns>Returns the internal cooldown for the Add Or Update Lobby Attributes endpoint.</returns>
        public TimeSpan GetAddOrUpdateLobbyAttributesCooldown()
        {
            return requestFactory.GetRequestCooldown(lobbiesResolveEndpoint + $"/{lobbyData.Id}/attrs", "PATCH");
        }

        /// <summary>
        /// Through this method you will be able to add new Attributes or update existing Attributes.
        /// Deleting existing Attributes is not supported, the list supplied as parameter will be merged with your
        /// current Attributes if the request succeeds with the backend.
        /// </summary>
        /// <param name="attributes">List of Attributes to be added or updated from the current Lobby.</param>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        public void AddOrUpdateLobbyAttributes(List<CloudAttribute> attributes, Action<RequestResponse<bool>> onRequestFinished)
        {
            var pathParams = $"/{lobbyData.Id}/attrs";

            var requestBody = SetAttributesRequest.GetRequestBody(attributes);

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "PATCH", requestBody,
                null, $"{nameof(LobbyOwnerSession)}.{nameof(AddOrUpdateLobbyAttributes)}", authClient.SessionToken, response =>
                {
                    var requestResponse = RequestResponse<bool>.GetRequestResponse(response);

                    if (response.Status == RequestStatus.Success)
                    {
                        requestResponse.Result = true;
                        lobbyData.MergeAttributesWith(attributes);
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
        /// <param name="attributes">List of Attributes to be added or updated from the current Lobby.</param>
        public async Task<bool> AddOrUpdateLobbyAttributesAsync(List<CloudAttribute> attributes)
        {
            var pathParams = $"/{lobbyData.Id}/attrs";

            var requestBody = SetAttributesRequest.GetRequestBody(attributes);

            var task = requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams, "PATCH",
                requestBody, null, $"{nameof(LobbyOwnerSession)}.{nameof(AddOrUpdateLobbyAttributesAsync)}", authClient.SessionToken);

            await task;

            if (task.Exception != null)
            {
                throw task.Exception;
            }

            lobbyData.MergeAttributesWith(attributes);

            return true;
        }

        /// <summary>Starts a game session for the current Lobby. A room will be created by coherence and supplied through the LobbiesService.OnPlaySessionStarted Callback.</summary>
        /// <param name="onRequestFinished">Callback that will be invoked when the request finished.</param>
        /// <param name="maxPlayers">Optional parameter to specify the max amount of Players allowed in the Room. If not supplied, the max amount of Players of the Lobby will be used.</param>
        /// <param name="unlistLobby">Optional parameter to unlist the Lobby. Unlisting the Lobby means that no other Player will be able to find the Lobby through matchmaking. True by default.</param>
        /// <param name="closeLobby">Optional parameter to close the Lobby. Closing the Lobby means that no other Player will be able to join the Lobby. False by default.</param>
        /// <param name="simPayload">Optional parameter to pass a custom payload to the simulator serving the room. None by default.</param>
        public void StartGameSession(Action<RequestResponse<bool>> onRequestFinished,
            int? maxPlayers = null, bool unlistLobby = true, bool closeLobby = false, string simPayload = null)
        {
            var pathParams = $"/{lobbyData.Id}/play";

            var requestBody = StartGameSessionRequest.GetRequestBody(maxPlayers ?? lobbyData.MaxPlayers,
                unlistLobby, closeLobby, simPayload);

            requestFactory.SendRequest(lobbiesResolveEndpoint, pathParams, "POST", requestBody,
                null, $"{nameof(LobbyOwnerSession)}.{nameof(StartGameSession)}", authClient.SessionToken, response =>
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

        /// <summary>Starts a game session for the current Lobby. A room will be created by coherence and supplied through the LobbiesService.OnPlaySessionStarted Callback.</summary>
        /// <param name="maxPlayers">Optional parameter to specify the max amount of Players allowed in the Room. If not supplied, the max amount of Players of the Lobby will be used.</param>
        /// <param name="unlistLobby">Optional parameter to unlist the Lobby. Unlisting the Lobby means that no other Player will be able to find the Lobby through matchmaking. True by default.</param>
        /// <param name="closeLobby">Optional parameter to close the Lobby. Closing the Lobby means that no other Player will be able to join the Lobby. False by default.</param>
        /// <param name="simPayload">Optional parameter to pass a custom payload to the simulator serving the room. None by default.</param>
        public async Task<bool> StartGameSessionAsync(
            int? maxPlayers = null, bool unlistLobby = true, bool closeLobby = false, string simPayload = null)
        {
            var pathParams = $"/{lobbyData.Id}/play";

            var requestBody = StartGameSessionRequest.GetRequestBody(maxPlayers ?? lobbyData.MaxPlayers,
                unlistLobby, closeLobby, simPayload);

            var task = requestFactory.SendRequestAsync(lobbiesResolveEndpoint, pathParams, "POST",
                requestBody, null, $"{nameof(LobbyOwnerSession)}.{nameof(StartGameSessionAsync)}", authClient.SessionToken);

            await task;

            if (task.Exception != null)
            {
                throw task.Exception;
            }

            return true;
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests.Lobbies
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Tests;
    using Moq;
    using NUnit.Framework;

    public class LobbyOwnerSessionTests : CoherenceTest
    {
        private Mock<IAuthClientInternal> authClientMock = new();
        private Mock<IRequestFactory> requestMock = new();

        private LobbyData lobbyData = new()
        {
            Id = "lobbyXYZ",
            Name = "mockLobby",
            Region = "us",
            Tag = "",
            MaxPlayers = 10,
            Closed = false,
            Unlisted = false,
            IsPrivate = false,
            ownerId = "player123",
            SimulatorSlug = "",
            RoomId = 10001,
            RoomData = new RoomData(),
            lobbyAttributes = new List<CloudAttribute>(),
        };

        private LobbyOwnerSession CreateLobbyOwnerSession() => new(lobbyData, authClientMock.Object, requestMock.Object);

        private string GetPlayerPath(LobbyPlayer player) => $"/{lobbyData.Id}/players/{player.Id}";
        private string GetAttributesPath() => $"/{lobbyData.Id}/attrs";
        private string GetStartSessionPath() => $"/{lobbyData.Id}/play";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            lobbyData.lobbyAttributes.Clear();
        }

        [Test(Description = "KickPlayer should send a DELETE request to the lobbies endpoint with the player's Id.")]
        [TestCase(true)]
        [TestCase(false)]
        public void KillPlayer(bool success)
        {
            // Arrange
            var lobbyOwnerSession = CreateLobbyOwnerSession();
            var player = new LobbyPlayer("player123");
            var requestResponse = new RequestResponse<string>
            {
                Result = "",
                Status = success ? RequestStatus.Success : RequestStatus.Fail
            };
            requestMock.Setup(x => x.SendRequest(
                    "/lobbies",
                    GetPlayerPath(player),
                    "DELETE",
                    null,
                    null,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>())).Callback<string, string, string, string, Dictionary<string, string>, string, string, Action<RequestResponse<string>>>((_, _, _, _, _, _, _, callback) =>
                    {
                        callback(requestResponse);
                    });

            // Act
            lobbyOwnerSession.KickPlayer(player, response =>
            {
                // Assert
                Assert.That(response.Result, Is.EqualTo(success));
            });
        }

        [Test(Description = "KickPlayerAsync should send a DELETE request to the lobbies endpoint with the player's Id.")]
        [TestCase(true)]
        [TestCase(false)]
        public async Task KickPlayerAsync(bool success)
        {
            // Arrange
            var lobbyOwnerSession = CreateLobbyOwnerSession();
            var player = new LobbyPlayer("player123");
            var requestTask = requestMock.Setup(x => x.SendRequestAsync(
                "/lobbies",
                GetPlayerPath(player),
                "DELETE",
                null,
                null,
                It.IsAny<string>(),
                It.IsAny<string>()));

            if (success)
            {
                requestTask.ReturnsAsync("");
            }
            else
            {
                requestTask.ThrowsAsync(new Exception("Test exception"));
            }

            // Act
            var result = await Task.Run(async () =>
            {
                try
                {
                    return await lobbyOwnerSession.KickPlayerAsync(player);
                }
                catch (Exception)
                {
                    return false;
                }
            });

            // Assert
            Assert.That(result, Is.EqualTo(success));
        }

        [Test(Description = "AddOrUpdateLobbyAttributes should send a PATCH request to the lobbies endpoint with the attributes and merge the attributes if successful.")]
        [TestCase(true)]
        [TestCase(false)]
        public void AddOrUpdateLobbyAttributes_MergesLobbyAttributes(bool success)
        {
            // Arrange
            var lobbyOwnerSession = CreateLobbyOwnerSession();
            var attributes = new List<CloudAttribute>
            {
                new ("name", "joe"),
                new ("score", "5")
            };
            var requestResponse = new RequestResponse<string>
            {
                Result = "",
                Status = success ? RequestStatus.Success : RequestStatus.Fail,
            };
            requestMock.Setup(x => x.SendRequest(
                    "/lobbies",
                    GetAttributesPath(),
                    "PATCH",
                    SetAttributesRequest.GetRequestBody(attributes),
                    null,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>())).Callback<string, string, string, string, Dictionary<string, string>, string, string, Action<RequestResponse<string>>>((_, _, _, _, _, _, _, callback) =>
                    {
                        callback(requestResponse);
                    });

            // Act
            lobbyOwnerSession.AddOrUpdateLobbyAttributes(attributes, response =>
            {
                // Assert
                Assert.That(response.Result, Is.EqualTo(success));
                if (success)
                {
                    Assert.That(lobbyData.Attributes, Is.EquivalentTo(attributes));
                }
                else
                {
                    Assert.That(lobbyData.Attributes, Is.Empty);
                }
            });
        }

        [Test(Description = "AddOrUpdateLobbyAttributesAsync should send a PATCH request to the lobbies endpoint with the attributes and merge the attributes if successful.")]
        [TestCase(true)]
        [TestCase(false)]
        public async Task AddOrUpdateLobbyAttributesAsync_MergesLobbyAttributes(bool success)
        {
            // Arrange
            var lobbyOwnerSession = CreateLobbyOwnerSession();
            var attributes = new List<CloudAttribute>
            {
                new ("name", "joe"),
                new ("score", "5")
            };
            var requestTask = requestMock.Setup(x => x.SendRequestAsync(
                "/lobbies",
                GetAttributesPath(),
                "PATCH",
                SetAttributesRequest.GetRequestBody(attributes),
                null,
                It.IsAny<string>(),
                It.IsAny<string>()));

            if (success)
            {
                requestTask.ReturnsAsync("");
            }
            else
            {
                requestTask.ThrowsAsync(new Exception("Test exception"));
            }

            // Act
            var result = await Task.Run(async () =>
            {
                try
                {
                    return await lobbyOwnerSession.AddOrUpdateLobbyAttributesAsync(attributes);
                }
                catch (Exception)
                {
                    return false;
                }
            });

            // Assert
            Assert.That(result, Is.EqualTo(success));
            if (success)
            {
                Assert.That(lobbyData.Attributes, Is.EquivalentTo(attributes));
            }
            else
            {
                Assert.That(lobbyData.Attributes, Is.Empty);
            }
        }

        [Test(Description = "StartGameSession should send a POST request to the lobbies endpoint with the desired parameters.")]
        [TestCase(true)]
        [TestCase(false)]
        public void StartGameSession(bool success)
        {
            // Arrange
            var lobbyOwnerSession = CreateLobbyOwnerSession();
            var requestResponse = new RequestResponse<string>
            {
                Result = "",
                Status = success ? RequestStatus.Success : RequestStatus.Fail,
            };
            var unlistLobby = true;
            var closeLobby = false;
            requestMock.Setup(x => x.SendRequest(
                    "/lobbies",
                    GetStartSessionPath(),
                    "POST",
                    StartGameSessionRequest.GetRequestBody(lobbyData.MaxPlayers, unlistLobby, closeLobby, null),
                    null,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>())).Callback<string, string, string, string, Dictionary<string, string>, string, string, Action<RequestResponse<string>>>((_, _, _, _, _, _, _, callback) =>
                    {
                        callback(requestResponse);
                    });

            // Act
            lobbyOwnerSession.StartGameSession(response =>
            {
                // Assert
                Assert.That(response.Result, Is.EqualTo(success));
            }, null, unlistLobby, closeLobby);
        }

        // create the same test as before but for async
        [Test(Description = "StartGameSessionAsync should send a POST request to the lobbies endpoint with the desired parameters.")]
        [TestCase(true)]
        [TestCase(false)]
        public async Task StartGameSessionAsync(bool success)
        {
            // Arrange
            var lobbyOwnerSession = CreateLobbyOwnerSession();
            var requestTask = requestMock.Setup(x => x.SendRequestAsync(
                "/lobbies",
                GetStartSessionPath(),
                "POST",
                StartGameSessionRequest.GetRequestBody(lobbyData.MaxPlayers, true, false, null),
                null,
                It.IsAny<string>(),
                It.IsAny<string>()));

            if (success)
            {
                requestTask.ReturnsAsync("");
            }
            else
            {
                requestTask.ThrowsAsync(new Exception("Test exception"));
            }

            // Act
            var result = await Task.Run(async () =>
            {
                try
                {
                    return await lobbyOwnerSession.StartGameSessionAsync();
                }
                catch (Exception)
                {
                    return false;
                }
            });

            // Assert
            Assert.That(result, Is.EqualTo(success));
        }
    }
}

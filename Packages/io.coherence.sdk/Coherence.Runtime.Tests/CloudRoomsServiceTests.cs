// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloud;
    using Common;
    using Moq;
    using NUnit.Framework;
    using Coherence.Tests;

    public class CloudRoomsServiceTests : CoherenceTest
    {
        private Mock<IAuthClientInternal> authClient = new();
        private Mock<IRuntimeSettings> runtimeSettings = new();

        private string roomsResponse =
            "{\"rooms\":[{\"room_id\":1,\"unique_id\":1766322077975,\"sdk_version\":\"v0.10.9\",\"host\":{\"ip\":\"18.196.114.164\",\"port\":31057,\"region\":\"eu\",\"rs_version\":\"v0.46.3\"},\"tags\":[],\"kv\":{\"name\":\"Room1\"},\"max_players\":10,\"connected_players\":0,\"sim_slug\":\"\"},{\"room_id\":3,\"unique_id\":1766338855191,\"sdk_version\":\"v0.10.9\",\"host\":{\"ip\":\"18.196.114.164\",\"port\":31057,\"region\":\"eu\",\"rs_version\":\"v0.46.3\"},\"tags\":[],\"kv\":{\"name\":\"Room2\"},\"max_players\":10,\"connected_players\":0,\"sim_slug\":\"\"}]}";

        private string emptyResponse = "{\"rooms\":[]}";

        private string createRoomResponse =
            "{\"delete_room_token\":\"70ef923f\",\"room_id\":4,\"unique_id\":1766422741271,\"sdk_version\":\"v0.10.9\",\"host\":{\"ip\":\"18.196.114.164\",\"port\":31057,\"region\":\"eu\",\"rs_version\":\"v0.46.3\"},\"tags\":[],\"kv\":{\"name\":\"\"},\"max_players\":10,\"connected_players\":0,\"sim_slug\":\"\",\"secret\":\"70ef923f\"}";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            runtimeSettings.Setup(settings => settings.SchemaID).Returns(string.Empty);
            var versionInfoMock = new Mock<IVersionInfo>(MockBehavior.Strict);
            versionInfoMock.Setup(vi => vi.Engine).Returns(string.Empty);
            runtimeSettings.Setup(settings => settings.VersionInfo).Returns(versionInfoMock.Object);
            runtimeSettings.Setup(settings => settings.SimulatorSlug).Returns(string.Empty);
            authClient.SetupGet(ac => ac.SessionToken).Returns(SessionToken.None);
        }

        private static void SetupRequestCallback(Mock<IRequestFactoryInternal> requestMock,
            Action<string, string, string, string, Dictionary<string, string>, string, string,
                Action<RequestResponse<string>>> callback)
        {
            requestMock.Setup(factory => factory.SendRequest(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback(callback);
        }

        private static void SetupRequestAsyncCallback(Mock<IRequestFactoryInternal> requestMock,
            Action<string, string, string, string, Dictionary<string, string>, string, string> callback)
        {
            requestMock.Setup(factory => factory.SendRequestAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Callback(callback);
        }

        private static void SetupRequestAsyncReturns(Mock<IRequestFactoryInternal> requestMock,
            Func<string, string, string, string, Dictionary<string, string>, string, string, Task<string>> callback)
        {
            requestMock.Setup(factory => factory.SendRequestAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(callback);
        }

        [Test]
        public void Should_ReturnNoRooms_When_NoRoomsExist()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            SetupRequestCallback(requestMock, (_, _, _, _, _, _, _, cb) =>
            {
                var response = new RequestResponse<string>
                {
                    Result = emptyResponse,
                    Status = RequestStatus.Success,
                };

                cb.Invoke(response);
            });

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("foo", credentials, runtimeSettings.Object);

            var receivedRooms = false;

            roomsService.FetchRooms(list =>
            {
                if (list.Result.Count == 0)
                {
                    receivedRooms = true;
                }
            });

            Assert.IsTrue(receivedRooms);
        }

        [Test]
        public void Should_ReturnTwoRooms_When_TwoRoomsExist()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            SetupRequestCallback(requestMock, (_, _, _, _, _, _, _, cb) =>
            {
                var response = new RequestResponse<string>
                {
                    Result = roomsResponse,
                    Status = RequestStatus.Success,
                };

                cb.Invoke(response);
            });

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            var receivedRooms = false;

            roomsService.FetchRooms(list =>
            {
                if (list.Result.Count == 2)
                {
                    receivedRooms = true;
                }
            });

            Assert.IsTrue(receivedRooms);
        }

        [Test]
        public void Should_RethrowException_When_RequestHasException()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            SetupRequestCallback(requestMock, (_, _, _, _, _, _, _, cb) =>
            {
                var response = new RequestResponse<string>
                {
                    Exception = new RequestException(ErrorCode.Unknown),
                    Status = RequestStatus.Fail,
                };

                cb.Invoke(response);
            });

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            var caughtException = false;

            roomsService.FetchRooms(list =>
            {
                caughtException = list.Exception != null;
            });

            Assert.IsTrue(caughtException);
        }

        [Test]
        public async Task Should_ReturnTwoRooms_When_TwoRoomsExistAsync()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            SetupRequestAsyncReturns(requestMock, (_, _, _, _, _, _, _) => Task.FromResult(roomsResponse));

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            var response = await roomsService.FetchRoomsAsync();

            Assert.IsTrue(response.Count == 2);
        }

        [Test]
        public async Task Should_ReturnNoRooms_When_NoRoomsExistAsync()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            SetupRequestAsyncReturns(requestMock, (_, _, _, _, _, _, _) => Task.FromResult(emptyResponse));

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            var response = await roomsService.FetchRoomsAsync();

            Assert.IsTrue(response.Count == 0);
        }

        [Test]
        public async Task Should_RethrowException_When_RequestHasExceptionAsync()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            SetupRequestAsyncCallback(requestMock,
                (_, _, _, _, _, _, _) => throw new RequestException(ErrorCode.Unknown));

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            var caughtException = false;

            try
            {
                var task = roomsService.FetchRoomsAsync();

                await task;
            }
#pragma warning disable 0168
            catch (RequestException e)
#pragma warning restore 0168
            {
                caughtException = true;
            }
            finally
            {
                Assert.IsTrue(caughtException);
            }
        }

        [Test]
        public async Task Should_QueueCallbacks_When_RequestIsOnGoing()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            var callbackSignal = new TaskCompletionSource<bool>();
            SetupRequestCallback(requestMock, async (_, _, _, _, _, _, _, cb) =>
            {
                var response = new RequestResponse<string>
                {
                    Result = roomsResponse,
                    Status = RequestStatus.Success,
                };

                await callbackSignal.Task;

                cb.Invoke(response);
            });

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            var receivedRooms1 = false;
            roomsService.FetchRooms(list =>
            {
                receivedRooms1 = true;
            });

            // The callback should NOT been called yet as callbackSignal is still blocking
            Assert.IsFalse(receivedRooms1);

            // Remove the block from the response
            SetupRequestCallback(requestMock, (_, _, _, _, _, _, _, cb) =>
            {
                var response = new RequestResponse<string>
                {
                    Result = roomsResponse,
                    Status = RequestStatus.Success,
                };

                cb.Invoke(response);
            });

            // While the response doesn't block, the queue should
            var receivedRooms2 = false;
            roomsService.FetchRooms(list =>
            {
                receivedRooms2 = true;
            });

            Assert.IsFalse(receivedRooms2);

            // Unblock queue
            callbackSignal.SetResult(true);
            await callbackSignal.Task;

            // Callback queue should be processed
            Assert.IsTrue(receivedRooms1);
            Assert.IsTrue(receivedRooms2);
        }

        [Test]
        public void Should_CreateOneRoom_When_CreatingARoom()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            requestMock.Setup(factory => factory.SendRequest(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, Dictionary<string, string>, string, string,
                    Action<RequestResponse<string>>>(
                    (_, _, _, _, _, _, cb) =>
                    {
                        var response = new RequestResponse<string>
                        {
                            Result = createRoomResponse,
                            Status = RequestStatus.Success,
                        };

                        cb.Invoke(response);
                    });

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            RoomData roomCreated = default;

            roomsService.CreateRoom(room =>
            {
                roomCreated = room.Result;
            }, RoomCreationOptions.Default);

            var hasRoom = false;

            foreach (var room in roomsService.CachedRooms)
            {
                if (room.UniqueId == roomCreated.UniqueId)
                {
                    hasRoom = true;
                }
            }

            Assert.IsTrue(hasRoom);
        }

        [Test]
        public async Task Should_CreateOneRoom_When_CreatingARoomAsync()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            requestMock.Setup(factory => factory.SendRequestAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.FromResult(createRoomResponse));

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            var roomCreated = await roomsService.CreateRoomAsync(RoomCreationOptions.Default);

            var hasRoom = false;

            foreach (var room in roomsService.CachedRooms)
            {
                if (room.UniqueId == roomCreated.UniqueId)
                {
                    hasRoom = true;
                }
            }

            Assert.IsTrue(hasRoom);
        }

        [Test]
        public void Should_DeleteOneRoom_When_DeletingARoom()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            requestMock.Setup(factory => factory.SendRequest(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, Dictionary<string, string>, string, string,
                    Action<RequestResponse<string>>>(
                    (_, _, _, _, _, _, cb) =>
                    {
                        var response = new RequestResponse<string>
                        {
                            Result = createRoomResponse,
                            Status = RequestStatus.Success,
                        };

                        cb.Invoke(response);
                    });

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            RoomData roomCreated = default;

            roomsService.CreateRoom(room =>
            {
                roomCreated = room.Result;
            }, RoomCreationOptions.Default);

            SetupRequestCallback(requestMock, (_, _, _, _, _, _, _, cb) =>
            {
                var response = new RequestResponse<string>
                {
                    Result = string.Empty,
                    Status = RequestStatus.Success,
                };

                cb.Invoke(response);
            });

            roomsService.RemoveRoom(roomCreated.UniqueId, roomCreated.Secret, null);

            var hasRoom = false;

            foreach (var room in roomsService.CachedRooms)
            {
                if (room.UniqueId == roomCreated.UniqueId)
                {
                    hasRoom = true;
                }
            }

            Assert.IsFalse(hasRoom);
        }

        [Test]
        public async Task Should_DeleteOneRoom_When_DeletingARoomAsync()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            requestMock.Setup(factory => factory.SendRequestAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(Task.FromResult(createRoomResponse));

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var roomsService = new CloudRoomsService("mock", credentials, runtimeSettings.Object);

            var roomCreated = await roomsService.CreateRoomAsync(RoomCreationOptions.Default);

            SetupRequestAsyncReturns(requestMock, (_, _, _, _, _, _, _) => Task.FromResult(string.Empty));

            await roomsService.RemoveRoomAsync(roomCreated.UniqueId, roomCreated.Secret);

            var hasRoom = false;

            foreach (var room in roomsService.CachedRooms)
            {
                if (room.UniqueId == roomCreated.UniqueId)
                {
                    hasRoom = true;
                }
            }

            Assert.IsFalse(hasRoom);
        }
    }
}

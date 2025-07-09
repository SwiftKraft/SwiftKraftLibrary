// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using Cloud;
    using Common;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Coherence.Tests;

    public class ReplicationServerRoomsServiceTests : CoherenceTest
    {
        private Mock<IRuntimeSettings> runtimeSettings = new Mock<IRuntimeSettings>();

        private string roomsResponse = "{\"rooms\":[{\"room_id\":1,\"unique_id\":1766322077975,\"sdk_version\":\"v0.10.9\",\"host\":{\"ip\":\"18.196.114.164\",\"port\":31057,\"region\":\"eu\",\"rs_version\":\"v0.46.3\"},\"tags\":[],\"kv\":{\"name\":\"Room1\"},\"max_players\":10,\"connected_players\":0,\"sim_slug\":\"\"},{\"room_id\":3,\"unique_id\":1766338855191,\"sdk_version\":\"v0.10.9\",\"host\":{\"ip\":\"18.196.114.164\",\"port\":31057,\"region\":\"eu\",\"rs_version\":\"v0.46.3\"},\"tags\":[],\"kv\":{\"name\":\"Room2\"},\"max_players\":10,\"connected_players\":0,\"sim_slug\":\"\"}]}";

        private string emptyResponse = "{\"rooms\":[]}";

        private string createRoomResponse = "{\"delete_room_token\":\"70ef923f\",\"room_id\":4,\"unique_id\":1766422741271,\"sdk_version\":\"v0.10.9\",\"host\":{\"ip\":\"18.196.114.164\",\"port\":31057,\"region\":\"eu\",\"rs_version\":\"v0.46.3\"},\"tags\":[],\"kv\":{\"name\":\"\"},\"max_players\":10,\"connected_players\":0,\"sim_slug\":\"\",\"secret\":\"70ef923f\"}";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            runtimeSettings.Setup(settings => settings.SchemaID).Returns(string.Empty);
            var versionInfoMock = new Mock<IVersionInfo>();
            versionInfoMock.Setup(vi => vi.Engine).Returns(string.Empty);
            runtimeSettings.Setup(settings => settings.VersionInfo).Returns(versionInfoMock.Object);
            runtimeSettings.Setup(settings => settings.SimulatorSlug).Returns(string.Empty);
        }

        [Test]
        public void Should_ReturnNoRooms_When_NoRoomsExist()
        {
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequest(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string, Action<RequestResponse<string>>>(
                    (s, s1, s2, arg3, arg4) =>
                    {
                        var response = new RequestResponse<string>()
                        {
                            Result = emptyResponse,
                            Status = RequestStatus.Success
                        };

                        arg4.Invoke(response);
                    });

            var roomsService = new ReplicationServerRoomsService("foo", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            bool receivedRooms = false;

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
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequest(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string, Action<RequestResponse<string>>>(
                    (s, s1, s2, arg3, arg4) =>
                    {
                        var response = new RequestResponse<string>()
                        {
                            Result = roomsResponse,
                            Status = RequestStatus.Success
                        };

                        arg4.Invoke(response);
                    });

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            bool receivedRooms = false;

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
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequest(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string, Action<RequestResponse<string>>>(
                    (s, s1, s2, arg3, arg4) =>
                    {
                        var response = new RequestResponse<string>()
                        {
                            Exception = new RequestException(ErrorCode.Unknown),
                            Status = RequestStatus.Fail
                        };

                        arg4.Invoke(response);
                    });

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            bool caughtException = false;

            roomsService.FetchRooms(list =>
            {
                caughtException = list.Exception != null;
            });

            Assert.IsTrue(caughtException);
        }

        [Test]
        public async Task Should_ReturnTwoRooms_When_TwoRoomsExistAsync()
        {
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequestAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<string>(roomsResponse));

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            var response = await roomsService.FetchRoomsAsync();

            Assert.IsTrue(response.Count == 2);
        }

        [Test]
        public async Task Should_ReturnNoRooms_When_NoRoomsExistAsync()
        {
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequestAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<string>(emptyResponse));

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            var response = await roomsService.FetchRoomsAsync();

            Assert.IsTrue(response.Count == 0);
        }

        [Test]
        public async Task Should_RethrowException_When_RequestHasExceptionAsync()
        {
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequestAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string, string>((s, s1, s2,
                    arg3) => throw new RequestException(ErrorCode.Unknown));

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            bool caughtException = false;

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
        public void Should_QueueCallbacks_When_RequestIsOnGoing()
        {
            var requestCount = 0;
            var responseReady = new TaskCompletionSource<bool>();

            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequest(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string,
                    Action<RequestResponse<string>>>(async (s, s1, s2, arg3, arg4) =>
                {
                    requestCount++;

                    var response = new RequestResponse<string>()
                    {
                        Result = roomsResponse,
                        Status = RequestStatus.Success
                    };

                    await responseReady.Task;

                    arg4.Invoke(response);
                });

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            var callbackCount = 0;
            roomsService.FetchRooms(list =>
            {
                callbackCount++;
            });

            roomsService.FetchRooms(list =>
            {
                callbackCount++;
            });

            Assert.That(requestCount, Is.EqualTo(1));
            Assert.That(callbackCount, Is.EqualTo(0));

            responseReady.SetResult(true);

            Assert.That(requestCount, Is.EqualTo(1));
            Assert.That(callbackCount, Is.EqualTo(2));
        }

        [Test]
        public void Should_CreateOneRoom_When_CreatingARoom()
        {
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequest(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string,
                    Action<RequestResponse<string>>>(
                    (s, s1, arg3, arg4, arg5) =>
                    {
                        var response = new RequestResponse<string>()
                        {
                            Result = createRoomResponse,
                            Status = RequestStatus.Success
                        };

                        arg5.Invoke(response);
                    });

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            RoomData roomCreated = default;

            roomsService.CreateRoom(room =>
            {
                roomCreated = room.Result;
            }, RoomCreationOptions.Default);

            bool hasRoom = false;

            foreach (RoomData room in roomsService.CachedRooms)
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
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequestAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<string>(createRoomResponse));

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            RoomData roomCreated = await roomsService.CreateRoomAsync(RoomCreationOptions.Default);

            bool hasRoom = false;

            foreach (RoomData room in roomsService.CachedRooms)
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
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequest(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string,
                    Action<RequestResponse<string>>>(
                    (s, s1, arg3, arg4, arg5) =>
                    {
                        var response = new RequestResponse<string>()
                        {
                            Result = createRoomResponse,
                            Status = RequestStatus.Success
                        };

                        arg5.Invoke(response);
                    });

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            RoomData roomCreated = default;

            roomsService.CreateRoom(room =>
            {
                roomCreated = room.Result;
            }, RoomCreationOptions.Default);

            requestMock.Setup(factory => factory.SendCustomRequest(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string,
                    Action<RequestResponse<string>>>(
                    (s, s1, arg3, arg4, arg5) =>
                    {
                        var response = new RequestResponse<string>()
                        {
                            Result = string.Empty,
                            Status = RequestStatus.Success
                        };

                        arg5.Invoke(response);
                    });

            roomsService.RemoveRoom(roomCreated.UniqueId, roomCreated.Secret, null);

            bool hasRoom = false;

            foreach (RoomData room in roomsService.CachedRooms)
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
            var requestMock = new Mock<IRequestFactory>();
            requestMock.Setup(factory => factory.SendCustomRequestAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<string>(createRoomResponse));

            var roomsService = new ReplicationServerRoomsService("mock", requestFactory: requestMock.Object, runtimeSettings: runtimeSettings.Object);

            RoomData roomCreated = await roomsService.CreateRoomAsync(RoomCreationOptions.Default);

            requestMock.Setup(factory => factory.SendCustomRequestAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<string>(string.Empty));

            await roomsService.RemoveRoomAsync(roomCreated.UniqueId, roomCreated.Secret);

            bool hasRoom = false;

            foreach (RoomData room in roomsService.CachedRooms)
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

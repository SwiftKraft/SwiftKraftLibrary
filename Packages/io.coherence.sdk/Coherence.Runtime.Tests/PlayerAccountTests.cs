// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Tests;
    using Coherence.Utils;
    using NUnit.Framework;
    using UnityEngine;
    using Utils;

    /// <summary>
    /// Unit tests for <see cref="PlayerAccount"/>.
    /// </summary>
    public sealed class PlayerAccountTests : CoherenceTest
    {
        private const string ProjectId = "ProjectId";
        private static CloudUniqueId UniqueId => new("CloudUniqueId");

        [TestCase(true), TestCase(false)]
        public async Task GetMainAsync_Can_Be_Canceled(bool waitUntilLoggedIn)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancelledOperation = PlayerAccount.GetMainAsync(waitUntilLoggedIn, cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();
            await cancelledOperation;
            Assert.That(cancelledOperation.IsCanceled, Is.True);
        }

        [Test]
        public void Main_Defaults_To_First_Registered_PlayerAccount()
        {
            using var playerAccountBuilder1 = CreateFakePlayerAccount(out var playerAccount1, uniqueId: "UniqueId1", autoLoginAsGuest: true);
            PlayerAccount.Register(playerAccount1);
            using var playerAccountBuilder2 = CreateFakePlayerAccount(out var playerAccount2, uniqueId: "UniqueId2", autoLoginAsGuest: false);
            PlayerAccount.Register(playerAccount2);
            Assert.That(PlayerAccount.Main, Is.EqualTo(playerAccount1));
            PlayerAccount.Unregister(playerAccount1);
            Assert.That(PlayerAccount.Main, Is.EqualTo(playerAccount2));
            PlayerAccount.Unregister(playerAccount2);
            Assert.That(PlayerAccount.Main, Is.Null);
        }

        [Test]
        public void SetAsMain_Sets_PlayerAccount_As_Main_PlayerAccount()
        {
            using var playerAccountBuilder1 = CreateFakePlayerAccount(out var playerAccount1, uniqueId: "UniqueId1", autoLoginAsGuest: false);
            using var playerAccountBuilder2 = CreateFakePlayerAccount(out var playerAccount2, uniqueId: "UniqueId2", autoLoginAsGuest: false);

            playerAccount1.SetAsMain();
            Assert.That(playerAccount1.IsMain, Is.True);
            Assert.That(playerAccount2.IsMain, Is.False);

            playerAccount2.SetAsMain();
            Assert.That(playerAccount1.IsMain, Is.False);
            Assert.That(playerAccount2.IsMain, Is.True);
        }

        [Test]
        public void Register_Adds_PlayerAccount_To_All()
        {
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount, autoLoginAsGuest: false);
            PlayerAccount.Register(playerAccount);
            Assert.That(PlayerAccount.All, Is.EquivalentTo(new[] { playerAccount }));
        }

        [Test]
        public void Unregister_Removes_PlayerAccount_From_All()
        {
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount, autoLoginAsGuest: false);
            PlayerAccount.Register(playerAccount);
            PlayerAccount.Unregister(playerAccount);
            Assert.That(PlayerAccount.All, Has.Length.Zero);
        }

        [Test]
        public async Task GetInfo_Request_Contains_Expected_Info()
        {
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.GetInfo();

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var expected = IPlayerAccountOperationRequest.GetAccountInfo();
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.pathParams, Is.EqualTo(expected.PathParams));
        }

        [TestCase(true), TestCase(false)]
        public async Task LinkSteam_Request_Contains_Provided_Info(bool force)
        {
            const string ticket = "ticket";
            const string identity = "identity";
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.LinkSteam(ticket, identity, force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var request = CoherenceJson.DeserializeObject<LinkSteamRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);
            var expected = IPlayerAccountOperationRequest.LinkSteam(ticket, identity, force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(request.Ticket, Is.EqualTo(ticket));
            Assert.That(request.Identity, Is.EqualTo(identity));
            Assert.That(request.Force, Is.EqualTo(force));
        }

        [TestCase(true), TestCase(false)]
        public async Task LinkEpicGames_Request_Contains_Provided_Info(bool force)
        {
            const string token = "token";
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.LinkEpicGames(token, force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var request = CoherenceJson.DeserializeObject<LinkEpicGamesRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);
            var expected = IPlayerAccountOperationRequest.LinkEpicGames(token, force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(request.Token, Is.EqualTo(token));
            Assert.That(request.Force, Is.EqualTo(force));
        }

        [TestCase(true), TestCase(false)]
        public async Task UnlinkSteam_Request_Contains_Expected_Info(bool force)
        {
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.UnlinkSteam(force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var expected = IPlayerAccountOperationRequest.UnlinkSteam(force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.pathParams, Is.EqualTo(expected.PathParams));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.body, Is.Null.Or.Empty);
        }

        [TestCase(true), TestCase(false)]
        public async Task LinkPlayStation_Request_Contains_Provided_Info(bool force)
        {
            const string token = "token";
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.LinkPlayStation(token, force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var request = CoherenceJson.DeserializeObject<LinkPlayStationRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);
            var expected = IPlayerAccountOperationRequest.LinkPlayStation(token, force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(request.Token, Is.EqualTo(token));
            Assert.That(request.Force, Is.EqualTo(force));
        }

        [TestCase(true), TestCase(false)]
        public async Task UnlinkPlayStation_Request_Contains_Expected_Info(bool force)
        {
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.UnlinkPlayStation(force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var expected = IPlayerAccountOperationRequest.UnlinkPlayStation(force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.pathParams, Is.EqualTo(expected.PathParams));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.body, Is.Null.Or.Empty);
        }

        [TestCase(true), TestCase(false)]
        public async Task LinkXbox_Request_Contains_Provided_Info(bool force)
        {
            const string token = "token";
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.LinkXbox(token, force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var request = CoherenceJson.DeserializeObject<LinkXboxRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);
            var expected = IPlayerAccountOperationRequest.LinkXbox(token, force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(request.Token, Is.EqualTo(token));
            Assert.That(request.Force, Is.EqualTo(force));
        }

        [TestCase(true), TestCase(false)]
        public async Task UnlinkXbox_Request_Contains_Expected_Info(bool force)
        {
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.UnlinkXbox(force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var expected = IPlayerAccountOperationRequest.UnlinkXbox(force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.pathParams, Is.EqualTo(expected.PathParams));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.body, Is.Null.Or.Empty);
        }

        [TestCase(true), TestCase(false)]
        public async Task LinkNintendo_Request_Contains_Provided_Info(bool force)
        {
            const string token = "token";
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.LinkNintendo(token, force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var request = CoherenceJson.DeserializeObject<LinkNintendoRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);
            var expected = IPlayerAccountOperationRequest.LinkNintendo(token, force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(request.Token, Is.EqualTo(token));
            Assert.That(request.Force, Is.EqualTo(force));
        }

        [TestCase(true), TestCase(false)]
        public async Task UnlinkNintendo_Request_Contains_Expected_Info(bool force)
        {
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.UnlinkNintendo(force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var expected = IPlayerAccountOperationRequest.UnlinkNintendo(force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.pathParams, Is.EqualTo(expected.PathParams));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.body, Is.Null.Or.Empty);

        }

        [TestCase(true), TestCase(false)]
        public async Task LinkJwt_Request_Contains_Provided_Info(bool force)
        {
            const string token = "token";
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.LinkJwt(token, force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var request = CoherenceJson.DeserializeObject<LinkJwtRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);
            var expected = IPlayerAccountOperationRequest.LinkJwt(token, force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(request.Token, Is.EqualTo(token));
            Assert.That(request.Force, Is.EqualTo(force));
        }

        [TestCase(true), TestCase(false)]
        public async Task UnlinkJwt_Request_Contains_Expected_Info(bool force)
        {
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.UnlinkJwt(force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var expected = IPlayerAccountOperationRequest.UnlinkJwt(force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.pathParams, Is.EqualTo(expected.PathParams));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.body, Is.Null.Or.Empty);
        }

        [TestCase(true), TestCase(false)]
        public async Task LinkGuest_Request_Contains_Provided_Info(bool force)
        {
            const string guestId = "guestId";
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);

            await playerAccount.LinkGuest(guestId, force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var request = CoherenceJson.DeserializeObject<LinkGuestRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);
            var expected = IPlayerAccountOperationRequest.LinkGuest(guestId, force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(request.GuestId, Is.EqualTo(guestId));
            Assert.That(request.Force, Is.EqualTo(force));
        }

        [TestCase(true), TestCase(false)]
        public async Task UnlinkGuest_Request_Contains_Expected_Info(bool force)
        {
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount, autoLoginAsGuest: true);
            var guestId = playerAccount.GuestId;

            await playerAccount.UnlinkGuest(force);

            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var expected = IPlayerAccountOperationRequest.UnlinkGuest(guestId, force);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(expected.BasePath));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.method, Is.EqualTo(expected.Method));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.pathParams, Is.EqualTo(expected.PathParams));
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.body, Is.Null.Or.Empty);
        }

        [Test]
        public async Task LoginAsGuest_Handles_LegacyLoginData_Properly()
        {
            const string username = "username";
            const string password = "password";
            GuestId.Delete(ProjectId, UniqueId);
            LegacyLoginData.SetCredentials(ProjectId, UniqueId, username, password);
            using var playerAccountBuilder = CreateFakePlayerAccount(out _, autoLoginAsGuest: false, mockAuthClient: false);
            var authClient = playerAccountBuilder.CloudServiceBuilder.AuthClient;
            var requestFactory = playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder;
            var expectedGuestId = GuestId.FromLegacyLoginData(username, password);

            var loginOperation = authClient.LoginAsGuest();

            // First should send LoginRequest containing legacy guest username and password
            var firstRequest = CoherenceJson.DeserializeObject<PasswordLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.LegacyGuest.GetBasePath()));
            Assert.That(firstRequest.Username, Is.EqualTo(username));
            Assert.That(firstRequest.Password, Is.EqualTo(password));

            await loginOperation;

            // Eventually should send LoginRequest containing guest id generated based on legacy login data and auto-signup set to true
            var secondRequest = CoherenceJson.DeserializeObject<LinkGuestRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);
            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(IPlayerAccountOperationRequest.LinkGuest(expectedGuestId, true).BasePath));
            Assert.That(secondRequest.GuestId, Is.EqualTo(expectedGuestId.ToString()));
            Assert.That(secondRequest.Force, Is.True);

            var returnedPlayerAccount = loginOperation.Result.PlayerAccount;
            Assert.That(returnedPlayerAccount.IsGuest, Is.True);

            var loginInfos = returnedPlayerAccount.LoginInfos;
            Assert.That(loginInfos.Count, Is.EqualTo(2));

            var legacyGuestLoginInfo = loginInfos.Single(x => x.LoginType == LoginType.LegacyGuest);
            Assert.That(legacyGuestLoginInfo.LoginType, Is.EqualTo(LoginType.LegacyGuest));
            Assert.That(legacyGuestLoginInfo.Username, Is.EqualTo(username));
            Assert.That(legacyGuestLoginInfo.Password, Is.EqualTo(password));

            var guestLoginInfo = loginInfos.Single(x => x.LoginType == LoginType.Guest);
            Assert.That(returnedPlayerAccount.IsGuest, Is.True);
            Assert.That(guestLoginInfo.LoginType, Is.EqualTo(LoginType.Guest));
            Assert.That(guestLoginInfo.GuestId, Is.EqualTo(expectedGuestId));
        }

        [Test]
        public async Task LinkGuest_Fails_If_Not_Logged_In()
        {
            const string guestId = "guestId";
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);
            playerAccountBuilder.CloudServiceBuilder.AuthClient.Logout();

            var loginOperation = await playerAccount.LinkGuest(guestId);

            Assert.That(loginOperation.HasFailed);
            Assert.That(loginOperation.Error, Is.Not.Null);
            Assert.That(loginOperation.Error.Type, Is.EqualTo(PlayerAccountErrorType.NotLoggedIn));
        }

        [Test]
        public async Task LinkGuest_Fails_Gracefully_If_Request_Factory_Throws_Exception()
        {
            var expectedException = new RequestException(HttpStatusCode.NotFound, "Not Found");
            using var playerAccountBuilder = CreateFakePlayerAccount(out var playerAccount);
            playerAccountBuilder.CloudServiceBuilder.RequestFactoryBuilder.OnSendRequestAsyncCalled(expectedException);

            var loginOperation = playerAccount.LinkGuest(null);
            await Task.WhenAny(loginOperation, Watchdog());

            Assert.That(loginOperation.HasFailed);
            Assert.That(loginOperation.Error, Is.Not.Null);

            async Task Watchdog()
            {
                await TimeSpan.FromSeconds(1f);
                if (!loginOperation.IsCompleted)
                {
                    throw new("LoginOperation did not fail as expected.");
                }
            }
        }

        public override void TearDown()
        {
            foreach (var playerAccount in PlayerAccount.All)
            {
                Debug.LogError($"Test did not unregister playerAccount: {playerAccount}.");
                PlayerAccount.Unregister(playerAccount);
            }

            base.TearDown();
        }

        private static FakePlayerAccountBuilder CreateFakePlayerAccount(out PlayerAccount playerAccount, string uniqueId = null, bool autoLoginAsGuest = true, bool? mockAuthClient = default) => new FakePlayerAccountBuilder()
            .SetProjectId(ProjectId)
            .SetUniqueId(!string.IsNullOrEmpty(uniqueId) ? new(uniqueId) : UniqueId)
            .SetupCloudService(x => x
                .SetupRequestFactory(x => x.SetIsReady(true))
                .SetupAuthClient(x => x.SetIsLoggedIn(true))
                .SetAutoLoginAsGuest(autoLoginAsGuest)
                .SetShouldMockAuthClient(mockAuthClient.HasValue ? mockAuthClient.Value : !autoLoginAsGuest))
            .Build(out playerAccount);
    }
}

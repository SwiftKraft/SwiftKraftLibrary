// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Tests;
    using Coherence.Utils;
    using Common;
    using NUnit.Framework;
    using UnityEngine;

    public class AuthClientTests : CoherenceTest
    {
        private const string ProjectId = "ProjectId";
        private CloudUniqueId CloudUniqueId => new("CloudUniqueId");

        private NewPlayerAccountProvider playerAccountProvider;
        private MockRequestFactoryBuilder requestFactory;
        private SessionToken ExpectedSessionToken => new(MockRequestFactoryBuilder.DefaultSessionToken);
        private bool SendRequestAsyncWasCalled => requestFactory.SendRequestAsyncWasCalled;
        private (string basePath, string pathParams, string method, string body, Dictionary<string, string> headers, string requestName, string sessionToken) SendRequestAsyncWasCalledWith => requestFactory.SendRequestAsyncWasCalledWith;

        [Test]
        public void LoginWithPassword_Completes_Successfully()
        {
            using var authClient = CreateAuthClient(isReady:true);

            var loginTask = authClient.LoginWithPassword(default, default, default);

            Assert.That(SendRequestAsyncWasCalled, Is.True);
            Assert.That(loginTask.IsCompleted, Is.True);
            Assert.That(loginTask.Result.LoggedIn, Is.True);
        }

        [Test]
        public void LoginWithPassword_Result_Contains_Provided_Username()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string username = "username";
            var loginTask = authClient.LoginWithPassword(username, default, default);

            Assert.That(loginTask.Result.Username, Is.EqualTo(username));
        }

        [TestCase(true), TestCase(false)]
        public void LoginWithPassword_Request_Contains_Provided_Info(bool autoSignup)
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string username = "username";
            const string password = "password";
            _ = authClient.LoginWithPassword(username, password, autoSignup);

            var request = CoherenceJson.DeserializeObject<PasswordLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.Password.GetBasePath()));
            Assert.That(request.Username, Is.EqualTo(username));
            Assert.That(request.Password, Is.EqualTo(password));
            Assert.That(request.Autosignup, Is.EqualTo(autoSignup));
        }

        [Test]
        public void LoginAsGuest_Request_Uses_Previously_Saved_GuestId()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string guestId = "guest id";
            GuestId.Save(ProjectId, CloudUniqueId, guestId);
            _ = authClient.LoginAsGuest();

            var request = CoherenceJson.DeserializeObject<GuestLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.Guest.GetBasePath()));
            Assert.That(request.GuestId, Is.EqualTo(guestId));

            GuestId.Delete(ProjectId, CloudUniqueId);
        }

        [Test]
        public void LoginWithSteam_Request_Contains_Provided_Info()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string ticket = "ticket";
            const string identity = "identity";
            _ = authClient.LoginWithSteam(ticket, identity);

            var request = CoherenceJson.DeserializeObject<SteamLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.Steam.GetBasePath()));
            Assert.That(request.Ticket, Is.EqualTo(ticket));
            Assert.That(request.Identity, Is.EqualTo(identity));
        }

        [Test]
        public void LoginWithPlayStation_Request_Contains_Provided_Info()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string token = "token";
            _ = authClient.LoginWithPlayStation(token);

            var request = CoherenceJson.DeserializeObject<PlayStationLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.PlayStation.GetBasePath()));
            Assert.That(request.Token, Is.EqualTo(token));
        }

        [Test]
        public void LoginWithXbox_Request_Contains_Provided_Info()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string token = "token";
            _ = authClient.LoginWithXbox(token);

            var request = CoherenceJson.DeserializeObject<XboxLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.Xbox.GetBasePath()));
            Assert.That(request.Token, Is.EqualTo(token));
        }

        [Test]
        public void LoginWithNintendo_Request_Contains_Provided_Info()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string token = "token";
            _ = authClient.LoginWithNintendo(token);

            var request = CoherenceJson.DeserializeObject<NintendoLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.Nintendo.GetBasePath()));
            Assert.That(request.Token, Is.EqualTo(token));
        }

        [Test]
        public void LoginWithJwt_Request_Contains_Provided_Info()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string token = "token";
            _ = authClient.LoginWithJwt(token);

            var request = CoherenceJson.DeserializeObject<JwtLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.Jwt.GetBasePath()));
            Assert.That(request.Token, Is.EqualTo(token));
        }

        [Test]
        public void LoginWithOneTimeCode_Request_Contains_Provided_Info()
        {
            using var authClient = CreateAuthClient(isReady:true);

            const string code = "code";
            _ = authClient.LoginWithOneTimeCode(code);

            var request = CoherenceJson.DeserializeObject<OneTimeCodeLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(requestFactory.SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.OneTimeCode.GetBasePath()));
            Assert.That(request.Code, Is.EqualTo(code));
        }

        [Test]
        public void LoginAsGuest_Completes_Successfully()
        {
            using var authClient = CreateAuthClient(isReady:true);

            var loginTask = authClient.LoginAsGuest();

            Assert.That(SendRequestAsyncWasCalled, Is.True);
            Assert.That(loginTask.IsCompleted, Is.True);
            Assert.That(loginTask.Result.LoggedIn, Is.True);
        }

        [Test]
        public void LogsOut_After_Websocket_Disconnected() => Assert.Ignore("Not implemented yet.");

        [Test]
        public void GeneratesNewLegacyGuestAccount_When_Fresh_LoggingInAsGuest()
        {
            const string legacyUsername = "testUsername";
            const string legacyPassword = "testPassword";
            const string guestId = "testGuestId";
            LegacyLoginData.SetCredentials(ProjectId, CloudUniqueId, legacyUsername, legacyPassword);
            LegacyLoginData.Clear(ProjectId, CloudUniqueId);
            GuestId.Save(ProjectId, CloudUniqueId, guestId);
            GuestId.Delete(ProjectId, CloudUniqueId);

            using var authClient = CreateAuthClient(autoLoginAsGuest: true);

            var request = CoherenceJson.DeserializeObject<PasswordLoginRequest>(requestFactory.SendRequestAsyncWasCalledWith.body);

            Assert.That(authClient.LoggedIn, Is.True);
            Assert.That(request.Username, Is.Not.EqualTo(legacyUsername));
            Assert.That(request.Password, Is.Not.EqualTo(legacyPassword));
            Assert.That(GuestId.GetOrCreate(ProjectId, CloudUniqueId), Is.Not.EqualTo(guestId));
        }

        [Test]
        public void Should_AutoLoginAsGuest_When_Instantiating()
        {
            using var authClient = CreateAuthClient(autoLoginAsGuest: true);

            Assert.That(GetSessionToken(authClient), Is.EqualTo(ExpectedSessionToken));
            Assert.That(authClient.LoggedIn, Is.True);
        }

        [Test]
        public void Should_AutoConnect_When_Using_AutoLoginAsGuest()
        {
            using var authClient = CreateAuthClient(autoLoginAsGuest:true);

            Assert.That(GetSessionToken(authClient), Is.EqualTo(ExpectedSessionToken));
            Assert.That(authClient.LoggedIn, Is.True);
        }

        [Test]
        public void Should_CallAccountEndpoint_When_InstantiatingWithAutoLoginAsGuestAndRequestBuilderIsReady()
        {
            CreateAuthClient(autoLoginAsGuest:true, isReady:true);

            Assert.That(SendRequestAsyncWasCalled, Is.True);
            Assert.That(SendRequestAsyncWasCalledWith.basePath, Is.EqualTo(LoginType.Guest.GetBasePath()));
            Assert.That(SendRequestAsyncWasCalledWith.method, Is.EqualTo(AuthClient.LoginRequestMethod));
            Assert.That(SendRequestAsyncWasCalledWith.requestName, Is.EqualTo(AuthClient.LoginRequestName));
            Assert.That(SendRequestAsyncWasCalledWith.sessionToken, Is.Null.Or.Empty);
        }

        [Test]
        public void Should_RefreshLogin_When_InstantiatingWithCachedToken()
        {
            using var authClient = CreateAuthClient(autoLoginAsGuest:false, isReady:true);
            var expectedSessionToken = new SessionToken("ExpectedSessionToken");

            var result = authClient.LoginWithSessionToken(expectedSessionToken);

            Assert.That(result.IsCompleted, Is.True);
            Assert.That(result.Result.LoggedIn, Is.True);
            Assert.That(result.Result.SessionToken, Is.EqualTo(ExpectedSessionToken));
            Assert.That(GetSessionToken(authClient), Is.EqualTo(ExpectedSessionToken));
            Assert.That(authClient.LoggedIn, Is.True);
        }

        [Test]
        public void Dispose_Does_Not_Cause_Warnings_Or_Errors()
        {
            var authClient = CreateAuthClient(autoLoginAsGuest:false, isReady:true);
            (string condition, string stacktrace, LogType type)? loggedWarningOrError = null;
            Application.logMessageReceived += OnLogMessageReceived;
            try
            {
                authClient.Dispose();
            }
            finally
            {
                Application.logMessageReceived -= OnLogMessageReceived;
            }

            if (loggedWarningOrError is not null)
            {
                var (condition, stacktrace, type) = loggedWarningOrError.Value;
                Assert.Fail($"Unexpected message of type {type}: {condition}\n{stacktrace}");
            }

            void OnLogMessageReceived(string condition, string stacktrace, LogType type)
            {
                if (type is not LogType.Log)
                {
                    loggedWarningOrError = (condition, stacktrace, type);
                    Application.logMessageReceived -= OnLogMessageReceived;
                }
            }
        }

        [SetUp]
        public override void SetUp()
        {
            playerAccountProvider = new(ProjectId, CloudUniqueId);
            base.SetUp();
            requestFactory = new MockRequestFactoryBuilder()
                .SetSessionToken(ExpectedSessionToken)
                .SetIsReady();
        }

        [TearDown]
        public override void TearDown()
        {
            playerAccountProvider.Dispose();
            requestFactory = null;

            foreach (var playerAccount in PlayerAccount.All)
            {
                Log.Log.GetLogger<AuthClientTests>().Error(Log.Error.UnitTestMissingCleanUp, $"Test did not unregister player account: {playerAccount}.");
                PlayerAccount.Unregister(playerAccount);
            }

            base.TearDown();
        }

        private AuthClient CreateAuthClient(bool autoLoginAsGuest = false, bool isReady = false)
        {
            var factory = requestFactory.SetIsReady(isReady).Build();
            var result = AuthClient.ForPlayer(factory, playerAccountProvider);
            if (autoLoginAsGuest)
            {
                result.LoginAsGuest().Then(task => Assert.Fail(task.Exception.ToString()), TaskContinuationOptions.OnlyOnFaulted);
            }

            return result;
        }

        private static SessionToken GetSessionToken(AuthClient authClient) => ((IAuthClientInternal)authClient).SessionToken;
    }
}

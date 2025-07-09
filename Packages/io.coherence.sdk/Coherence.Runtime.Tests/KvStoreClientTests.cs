// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Tests;
    using Moq;
    using NUnit.Framework;

    public class KvStoreClientTests : CoherenceTest
    {
        private Mock<IAuthClientInternal> authClientMock = new(MockBehavior.Strict);
        private Mock<IRequestFactory> requestMock = new(MockBehavior.Strict);

        private KvStoreClient NewKvStoreClient() =>
            new (requestMock.Object, authClientMock.Object, registerForUpdate: false);

        [Test]
        public void OnLogin_ShouldSyncKvValues()
        {
           var client = NewKvStoreClient();

           var loginResponse = GetTestLoginResponse();
           authClientMock.Raise(mock => mock.OnLogin += null, loginResponse);

           Assert.AreEqual(client.Get("foo"), "bar");
        }

        [Test]
        public void OnLogout_ShouldClearKvValues()
        {
            var client = NewKvStoreClient();
            client.Set("foo", "bar");

            authClientMock.Raise(mock => mock.OnLogout += null);

            Assert.IsNull(client.Get("foo"));
        }

        [Test]
        public void Set_ShouldReturnTrue_WhenKeyAndValueAreValid()
        {
            var client = NewKvStoreClient();

            Assert.IsTrue(client.Set("foo", "bar"));
        }

        [Test]
        public void Set_ShouldReturnFalse_WhenKeyOrValueAreInvalid()
        {
            var client = NewKvStoreClient();

            Assert.IsFalse(client.Set("foo!", "bar"));
            Assert.IsFalse(client.Set("foo", null));
            Assert.IsFalse(client.Set(null, "bar"));
        }

        [Test]
        public void Unset_ShouldReturnTrue_WhenKeyExists()
        {
            var client = NewKvStoreClient();
            client.Set("foo", "bar");

            Assert.IsTrue(client.Unset("foo"));
        }

        [Test]
        public void Unset_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            var client = NewKvStoreClient();

            Assert.IsFalse(client.Unset("foo"));
        }

        [Test(Description = "Update sends an API request when there are changes in the local KV values")]
        public async Task Update_ShouldSendRequest_WhenDirty()
        {
            authClientMock.Setup(mock => mock.LoggedIn).Returns(true);
            authClientMock.Setup(mock => mock.SessionToken).Returns(new SessionToken("token"));

            requestMock.Setup(mock =>
                    mock.SendRequestAsync(
                        "/kv",
                        "POST",
                        It.IsAny<string>(),
                        null,
                        It.IsAny<string>(),
                        It.IsAny<string>())
                    )
                .ReturnsAsync("{}");

            var client = NewKvStoreClient();
            client.Set("foo", "bar");

            await DoClientUpdate(client);

            requestMock.Verify(mock =>
                mock.SendRequestAsync(
                    "/kv",
                    "POST",
                    It.IsAny<string>(),
                    null,
                    It.IsAny<string>(),
                    "token")
                , Times.Once);
        }

        [Test(Description = "Update does not send an API request when there are no changes in the local KV values")]
        public async Task Update_ShouldNotSendRequest_WhenNotDirty()
        {
            var client = NewKvStoreClient();

            authClientMock.Setup(mock => mock.LoggedIn).Returns(true);
            authClientMock.Setup(mock => mock.SessionToken).Returns(new SessionToken("token"));

            requestMock.Setup(mock =>
                    mock.SendRequestAsync(
                        "/kv",
                        "POST",
                        It.IsAny<string>(),
                        null,
                        It.IsAny<string>(),
                        It.IsAny<string>())
                    )
                .ReturnsAsync("{}");

            await DoClientUpdate(client);

            requestMock.Verify(mock =>
                mock.SendRequestAsync(
                    "/kv",
                    "POST",
                    It.IsAny<string>(),
                    null,
                    It.IsAny<string>(),
                    "token")
                , Times.Never);

        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            authClientMock.Reset();
            requestMock.Reset();
        }

        private async Task DoClientUpdate(KvStoreClient client)
        {
            var tcs = new TaskCompletionSource<object>();
            try
            {
                await Task.Delay(Constants.minBackoff); // Workaround: wait for backoff to expire
                client.Update();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }

        private LoginResponse GetTestLoginResponse() =>
            new LoginResponse
            {
                KvStoreState = new System.Collections.Generic.List<KvPair>
                {
                    new KvPair { Key = "foo", Value = "bar" }
                }
            };
    }
}

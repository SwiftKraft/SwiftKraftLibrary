// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Tests;
    using Coherence.Utils;
    using NUnit.Framework;

    /// <summary>
    /// Unit tests for <see cref="CloudStorage"/>.
    /// </summary>
    public sealed class CloudStorageTests : CoherenceTest
    {
        private static StorageItem Item1 => new(Key1, Value1);
        private static StorageItem Item2 => new(Key2, Value2);
        private static StorageItem Item3 => new(Key3, Value3);
        private static Key Key1 => "Key1";
        private static Key Key2 => "Key2";
        private static Key Key3 => "Key3";
        private static Value Value1 => "Value1";
        private static Value Value2 => "Value2";
        private static Value Value3 => "Value3";
        private static StorageObjectId ObjectId => (new("Test", Guid.NewGuid()));

        private FakeCloudStorageBuilder cloudStorageBuilder;
        private CloudStorage CloudStorage => cloudStorageBuilder.Build();
        private MockAuthClientBuilder MockAuthClient => cloudStorageBuilder.AuthClient;
        private MockRequestFactoryBuilder MockRequestFactory => cloudStorageBuilder.RequestFactory;

        public override void SetUp()
        {
            base.SetUp();
            cloudStorageBuilder = new();
        }

        [Test]
        public void IsReady_ReturnsTrue_WhenLoggedInAndRequestFactoryIsReadyAndNoOperationInProgress()
        {
            var isReady = CloudStorage.IsReady;

            Assert.That(isReady, Is.True);
        }

        [Test]
        public void IsReady_ReturnsFalse_WhenNotLoggedIn()
        {
            MockAuthClient.SetIsLoggedIn(false);

            var isReady = CloudStorage.IsReady;

            Assert.That(isReady, Is.False);
        }

        [Test]
        public void IsReady_ReturnsFalse_WhenRequestFactoryIsNotReady()
        {
            MockRequestFactory.SetIsReady(false);

            var isReady = CloudStorage.IsReady;

            Assert.That(isReady, Is.False);
        }

        [Test]
        public void LoadObjectAsync_ReturnsNotLoggedInError_WhenAuthClientIsNotLoggedIn()
        {
            MockAuthClient.SetIsLoggedIn(false);

            var result = CloudStorage.LoadObjectAsync<string>(ObjectId);

            Assert.That(result.Error.Type, Is.EqualTo(StorageErrorType.NotLoggedIn));
        }

        [Test]
        public async Task LoadObjectAsync_Deserializes_Dictionary_From_Json()
        {
            var objectId = ObjectId;
            var expectedResult = new Dictionary<string, string> { { Key1, Value1 }, { Key2, Value2 }, { Key3, Value3 } };

            MockRequestFactory.SetSendRequestAsyncReturns(CoherenceJson.SerializeObject(new CloudStorage.PayloadLoadRequest.Response
            {
                data = new[]
                {
                    new CloudStorage.PayloadStorageObject(objectId.Type, objectId.Id, expectedResult),
                }
            }, StorageObject.jsonConverters));

            var operation = await CloudStorage.LoadObjectAsync<Dictionary<string, string>>(objectId);

            Assert.That(operation.IsCompletedSuccessfully, Is.True, operation.Error?.Message);
            Assert.That(operation.Result, Is.EquivalentTo(expectedResult));
        }

        [Test]
        public async Task SaveObjectAsync_Serializes_Dictionary_As_Json()
        {
            var objectId = ObjectId;
            var dictionary = new Dictionary<string, string> { { Key1, Value1 }, { Key2, Value2 }, { Key3, Value3 } };
            var expectedBody = CoherenceJson.SerializeObject(new CloudStorage.PayloadSaveRequest
            {
                storageObjectMutations = new[]
                {
                    new CloudStorage.PayloadStorageObject
                    {
                        type = objectId.Type,
                        id = objectId.Id,
                        data = dictionary
                    }
                }
            }, StorageObject.jsonConverters);

            var operation = await CloudStorage.SaveObjectAsync(objectId, dictionary);

            Assert.That(operation.IsCompletedSuccessfully, Is.True, operation.Error?.Message);
            var actualBody = MockRequestFactory.SendRequestAsyncWasCalledWith.body;
            Assert.That(actualBody, Is.EqualTo(expectedBody));
        }

        [Test]
        public async Task LoadObjectAsync_Deserializes_String_From_Json()
        {
            var objectId = ObjectId;
            var expectedResult = "Test";

            MockRequestFactory.SetSendRequestAsyncReturns(CoherenceJson.SerializeObject(new CloudStorage.PayloadLoadRequest.Response
            {
                data = new[]
                {
                    new CloudStorage.PayloadStorageObject(objectId.Type, objectId.Id, expectedResult),
                }
            }, StorageObject.jsonConverters));

            var operation = await CloudStorage.LoadObjectAsync<string>(objectId);

            Assert.That(operation.IsCompletedSuccessfully, Is.True, operation.Error?.Message);
            Assert.That(operation.Result, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task SaveObjectAsync_Serializes_String_As_Json()
        {
            var objectId = ObjectId;
            var @string = "Test";
            var expectedBody = CoherenceJson.SerializeObject(new CloudStorage.PayloadSaveRequest
            {
                storageObjectMutations = new[]
                {
                    new CloudStorage.PayloadStorageObject
                    {
                        type = objectId.Type,
                        id = objectId.Id,
                        data = @string,
                    }
                }
            }, StorageObject.jsonConverters);

            var operation = await CloudStorage.SaveObjectAsync(objectId, @string);

            Assert.That(operation.IsCompletedSuccessfully, Is.True, operation.Error?.Message);
            var actualBody = MockRequestFactory.SendRequestAsyncWasCalledWith.body;
            Assert.That(actualBody, Is.EqualTo(expectedBody));
        }

        [Test]
        public async Task LoadObjectAsync_ReturnsRequestException_Thrown_By_RequestFactory()
        {
            var expectedException = new RequestException(123, "TestException");
            MockRequestFactory.OnSendRequestAsyncCalled(expectedException);

            var operation = await CloudStorage.LoadObjectAsync<string>(ObjectId);

            Assert.That(operation.HasFailed, Is.True);
            Assert.That(operation.Error.Type, Is.EqualTo(StorageErrorType.RequestException));
        }

        [Test]
        public async Task SaveObjectAsync_ReturnsNotLoggedInError_WhenAuthClientIsNotLoggedIn()
        {
            MockAuthClient.SetIsLoggedIn(false);

            var operation = await CloudStorage.SaveObjectAsync(ObjectId, Enumerable.Empty<StorageItem>());

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Error, Is.Not.Null);
            Assert.That(operation.Error.Type, Is.EqualTo(StorageErrorType.NotLoggedIn));
        }

        [Test]
        public async Task SaveObjectAsync_ReturnsRequestException_Thrown_By_RequestFactory()
        {
            var expectedException = new RequestException(123, "TestException");
            MockRequestFactory.OnSendRequestAsyncCalled(expectedException);

            var operation = await CloudStorage.SaveObjectAsync(ObjectId, Enumerable.Empty<StorageItem>());

            Assert.That(operation.Error.Type, Is.EqualTo(StorageErrorType.RequestException));
            Assert.That(operation.Error.RequestException, Is.EqualTo(expectedException));
        }

        [Test]
        public async Task DeleteObjectAsync_ReturnsNotLoggedInError_WhenAuthClientIsNotLoggedIn()
        {
            MockAuthClient.SetIsLoggedIn(false);

            var operation = await CloudStorage.DeleteObjectAsync(ObjectId);

            Assert.That(operation, Is.Not.Null);
            Assert.That(operation.Error, Is.Not.Null);
            Assert.That(operation.Error.Type, Is.EqualTo(StorageErrorType.NotLoggedIn));
        }

        [Test]
        public async Task DeleteObjectAsync_ReturnsRequestException_Thrown_By_RequestFactory()
        {
            var expectedException = new RequestException(123, "TestException");
            MockRequestFactory.OnSendRequestAsyncCalled(expectedException);

            var operation = await CloudStorage.DeleteObjectAsync(ObjectId);

            Assert.That(operation.Error.Type, Is.EqualTo(StorageErrorType.RequestException));
            Assert.That(operation.Error.RequestException, Is.EqualTo(expectedException));
        }

        [Test]
        public async Task CloudStorage_IsBusy_Until_SendRequestAsync_Completes_Successfully()
        {
            var objectId = ObjectId;

            MockRequestFactory.SetSendRequestAsyncReturns(async () =>
            {
                await Task.Yield();
                return CoherenceJson.SerializeObject(new CloudStorage.PayloadLoadRequest.Response
                {
                    data = new[] { new CloudStorage.PayloadStorageObject(objectId.Type, objectId.Id, "Test") }
                }, StorageObject.jsonConverters);
            });

            var operation = CloudStorage.LoadObjectAsync<string>(objectId);

            Assert.That(CloudStorage.IsBusy, Is.True);

            await operation;

            Assert.That(operation.IsCompletedSuccessfully, Is.True, operation.Error?.Message);
            Assert.That(CloudStorage.IsBusy, Is.False);
        }

        [Test]
        public async Task CloudStorage_IsBusy_Until_SendRequestAsync_Throws_An_Exception()
        {
            MockRequestFactory.SetSendRequestAsyncReturns(async () =>
            {
                await Task.Yield();
                throw new("TestException");
            });

            var operation = CloudStorage.LoadObjectAsync<string>(ObjectId);

            Assert.That(CloudStorage.IsBusy, Is.True);

            await operation;

            Assert.That(operation.HasFailed, Is.True);
            Assert.That(CloudStorage.IsBusy, Is.False);
        }

        [Test]
        public async Task CloudStorage_IsBusy_Until_Operation_Cancelled()
        {
            MockRequestFactory.SetSendRequestAsyncReturns(async () =>
            {
                await Task.Yield();
                return "";
            });

            var cancellationTokenSource = new CancellationTokenSource();

            var operation = CloudStorage.LoadObjectAsync<string>(ObjectId, cancellationTokenSource.Token);

            Assert.That(CloudStorage.IsBusy, Is.True);

            cancellationTokenSource.Cancel();

            await operation;

            Assert.That(operation.IsCanceled, Is.True);
            Assert.That(CloudStorage.IsBusy, Is.False);
        }

        [Test]
        public async Task SaveObjectAsync_Request_Contains_Expected_Data()
        {
            var objectId = ObjectId;
            await CloudStorage.SaveObjectAsync(objectId, "Test");

            var requestArgs = MockRequestFactory.SendRequestAsyncWasCalledWith;
            var request = CoherenceJson.DeserializeObject<CloudStorage.PayloadSaveRequest>(requestArgs.body, StorageObject.jsonConverters);
            var requestMutation = request.storageObjectMutations.Single();
            Assert.That(requestMutation.id, Is.EqualTo(objectId.Id));
            Assert.That(requestMutation.type, Is.EqualTo(objectId.Type));
            Assert.That(requestMutation.data, Is.EqualTo("Test"));
            Assert.That(requestArgs.method, Is.EqualTo(CloudStorage.SaveRequestMethod));
            Assert.That(requestArgs.pathParams, Is.Null.Or.Empty);
        }

        [Test]
        public async Task LoadObjectAsync_Request_Contains_Expected_Data()
        {
            var objectId = ObjectId;
            var operation = await CloudStorage.LoadObjectAsync<string>(objectId);
            operation.Error.Ignore();

            var requestArgs = MockRequestFactory.SendRequestAsyncWasCalledWith;
            var request = CoherenceJson.DeserializeObject<CloudStorage.PayloadLoadRequest>(requestArgs.body, StorageObject.jsonConverters);
            var requestId = request.object_ids.Single();
            Assert.That(requestId.id, Is.EqualTo(objectId.Id));
            Assert.That(requestId.type, Is.EqualTo(objectId.Type));
            Assert.That(requestArgs.method, Is.EqualTo(CloudStorage.LoadRequestMethod));
            Assert.That(requestArgs.pathParams, Is.Null.Or.Empty);
        }

        [Test]
        public async Task DeleteObjectAsync_Request_Contains_Expected_Data()
        {
            var objectId = ObjectId;
            await CloudStorage.DeleteObjectAsync(objectId);

            var requestArgs = MockRequestFactory.SendRequestAsyncWasCalledWith;
            var request = CoherenceJson.DeserializeObject<CloudStorage.PayloadDeleteRequest>(requestArgs.body, StorageObject.jsonConverters);
            var requestId = request.storageObjectIds.Single();
            Assert.That(requestId.id, Is.EqualTo(objectId.Id));
            Assert.That(requestId.type, Is.EqualTo(objectId.Type));
            Assert.That(requestArgs.method, Is.EqualTo(CloudStorage.DeleteRequestMethod));
            Assert.That(requestArgs.pathParams, Is.EqualTo(CloudStorage.DeletePathParams));
        }

        public override void TearDown()
        {
            ((IDisposable)CloudStorage).Dispose();
            base.TearDown();
        }
    }
}

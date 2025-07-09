// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using Common;
    using Log;
    using Runtime;

    /// <summary>
    /// Represents an error that occurred when interacting with the <see cref="CloudStorage"/> service.
    /// <para>
    /// If the error is never observed by the user, it will get automatically logged to the Console.
    /// </para>
    /// </summary>
    public sealed class StorageError : CoherenceError<StorageErrorType>
    {
        /// <summary>
        /// Gets the <see cref="RequestException"/> that caused the error, if any.
        /// <para>
        /// This contains the <see cref="RequestException.HttpStatusCode"/> that was
        /// received from the server in response to the request that was sent.
        /// </para>
        /// </summary>
        [MaybeNull]
        public RequestException RequestException { get; }

        internal StorageError(StorageErrorType type, string message, Error error = Error.UnobservedError, bool hasBeenObserved = false)
            : base(type, message, error, hasBeenObserved){ }

        internal StorageError([DisallowNull] RequestException requestException, StorageObjectId[] storageObjectIds, Error error = Error.UnobservedError, bool hasBeenObserved = false)
            : this(GetStorageErrorType(requestException), GetMessage(requestException, storageObjectIds), error, hasBeenObserved)
            => RequestException = requestException;

        private protected override Exception ToException() => new StorageException(Type, ToString(), RequestException);

        private static StorageErrorType GetStorageErrorType(RequestException requestException) => requestException.HttpStatusCode switch
        {
            HttpStatusCode.NotFound => StorageErrorType.ObjectNotFound,
            _ => StorageErrorType.RequestException
        };

        private static string GetMessage(RequestException requestException, StorageObjectId[] storageObjectIds) => requestException.HttpStatusCode switch
        {
            HttpStatusCode.Unauthorized => "You are unauthorized to perform this operation. Please make sure you have logged in to coherence Cloud in coherence Hub and have a project selected.\n" + requestException,
            HttpStatusCode.Forbidden => "You do not have permission to perform this operation. Please check that you have enabled the 'Persisted Player Accounts' service in the Project Setting section of your coherence Cloud Dashboard at https://coherence.io/dashboard.\n" + requestException,
            HttpStatusCode.NotFound => StorageException.GetStorageObjectNotFoundErrorMessage(storageObjectIds),
            HttpStatusCode.InternalServerError => "Internal Server Error. Please try again later.\n" + requestException,
            _ => requestException.ToString()
        };
    }
}

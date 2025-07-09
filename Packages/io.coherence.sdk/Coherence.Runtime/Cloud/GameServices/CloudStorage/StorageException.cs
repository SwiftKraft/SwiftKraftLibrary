// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Net;
    using Runtime;
    using Utils;

    /// <summary>
    /// An exception that can be thrown by <see cref="CloudStorage"/> and storage object related methods.
    /// <remarks>
    /// These should always be caught internally and converted into <see cref="StorageError"/> result objects
    /// for the user.
    /// </remarks>>
    /// </summary>
    internal sealed class StorageException : Exception
    {
        /// <summary>
        /// Error type to give the <see cref="StorageError"/> result objects that is generated from this exception.
        /// </summary>
        public StorageErrorType ErrorType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageException"/> class.
        /// </summary>
        /// <param name="errorType">
        /// Error type to give the <see cref="StorageError"/> result objects that is generated from this exception.
        /// </param>
        /// <param name="message"> A message describing what went wrong. </param>
        /// <param name="innerException"> (Optional) Internal exception that caused this exception. </param>
        internal StorageException(StorageErrorType errorType, string message, Exception innerException = null) : base(message, innerException) => ErrorType = errorType;

        internal static StorageException NotLoggedIn(StorageObjectId[] storageIds, string methodName)
            => new(StorageErrorType.NotLoggedIn, $"The user must be logged in to coherence Cloud before {methodName} can be used.\nStorage Object IDs: '{storageIds.AllToString(", ")}'");

        internal static StorageException StorageObjectNotFound(params StorageObjectId[] idsNotFound)
        {
            var message = GetStorageObjectNotFoundErrorMessage(idsNotFound);
            return new(StorageErrorType.ObjectNotFound, message, new RequestException(HttpStatusCode.NotFound, message));
        }

        internal static StorageException From(Exception exception, params StorageObjectId[] storageIds)
        {
            if (exception is null)
            {
                return null;
            }

            if (exception.TryExtract(out StorageException storageException))
            {
                return storageException;
            }

            if (exception.TryExtract(out RequestException requestException))
            {
                return new(GetErrorType(requestException), GetMessage(requestException, storageIds), requestException);
            }

            return new(StorageErrorType.None, exception.Message, exception);

            static StorageErrorType GetErrorType(RequestException requestException) => requestException switch
            {
                { HttpStatusCode: HttpStatusCode.NotFound } => StorageErrorType.ObjectNotFound,
                _ => StorageErrorType.RequestException
            };

            static string GetMessage(RequestException requestException, StorageObjectId[] storageIds) => requestException switch
            {
                { HttpStatusCode: HttpStatusCode.NotFound } => GetStorageObjectNotFoundErrorMessage(storageIds),
                _ => requestException.Message
            };
        }

        internal static string GetStorageObjectNotFoundErrorMessage(params StorageObjectId[] storageIds) => (storageIds?.Length ?? 0) switch
        {
            0 => "No object with the provided ID was found.",
            1 => $"No object with the provided ID was found: {storageIds[0]}.",
            _ => $"No objects with the provided IDs were found:\n{storageIds.AllToString("\n")}."
        };
    }
}

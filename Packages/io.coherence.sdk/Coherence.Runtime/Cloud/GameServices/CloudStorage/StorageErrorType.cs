// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    /// <summary>
    /// Specifies the different types of errors that can occur when interacting with the <see cref="CloudStorage"/> service.
    /// </summary>
    public enum StorageErrorType
    {
        /// <summary>
        /// No error has occurred.
        /// </summary>
        None,

        /// <summary>
        /// The user is not logged in.
        /// </summary>
        NotLoggedIn,

        /// <summary>
        /// An error occurred when trying to communicate with coherence Cloud.
        /// </summary>
        RequestException,

        /// <summary>
        /// An invalid <see cref="StorageObjectId">object identifier</see> was provided.
        /// </summary>
        InvalidObjectId,

        /// <summary>
        /// An invalid key for an item was provided.
        /// </summary>
        InvalidKey,

        /// <summary>
        /// An invalid value for an item was provided.
        /// </summary>
        InvalidValue,

        /// <summary>
        /// An attempt was made to retrieve a value with a given key from an object,
        /// but no item with that key existed.
        /// </summary>
        KeyNotFound,

        /// <summary>
        /// An attempt was made to load an object from cloud storage,
        /// but no object with the provided identifier was found.
        /// </summary>
        ObjectNotFound,

        /// <summary>
        /// A null argument was provided when a non-null argument was required.
        /// </summary>
        NullArgument,

        /// <summary>
        /// An empty collection was provided when one or more elements were required.
        /// </summary>
        EmptyArgument,

        /// <summary>
        /// The CloudStorage object has been disposed and can no longer be used.
        /// </summary>
        CloudStorageHasBeenDisposed,

        /// <summary>
        /// Serializing an object to be saved into JSON failed.
        /// </summary>
        SerializationFailed,

        /// <summary>
        /// Deserializing an object from loaded JSON data failed.
        /// </summary>
        DeserializationFailed,
    }
}

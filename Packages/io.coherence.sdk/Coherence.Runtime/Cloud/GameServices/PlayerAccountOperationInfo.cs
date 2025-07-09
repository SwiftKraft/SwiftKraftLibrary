// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Runtime;

    /// <summary>
    /// Specifies information used to perform an operation related to a <see cref="PlayerAccount"/>.
    /// </summary>
    internal readonly struct PlayerAccountOperationInfo<TRequest> where TRequest : struct, IPlayerAccountOperationRequest
    {
        public PlayerAccountOperationType OperationType { get; }
        public TRequest? Request { get; }
        public string BasePath { get; }
        public string PathParams { get; }
        public string Method { get; }

        public PlayerAccountOperationInfo(PlayerAccountOperationType operationType, string basePath, string method, TRequest? request, string pathParams = "")
        {
            OperationType = operationType;
            BasePath = basePath;
            Method = method;
            Request = request;
            PathParams = pathParams;
        }
    }
}

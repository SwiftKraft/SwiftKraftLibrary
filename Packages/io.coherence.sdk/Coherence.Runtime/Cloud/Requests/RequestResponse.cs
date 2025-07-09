// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;

    public struct RequestResponse<T>
    {
        public RequestStatus Status;
        public T Result;
        public Exception Exception;

        public static RequestResponse<T> GetRequestResponse(RequestResponse<string> response)
        {
            RequestResponse<T> requestResponse = new RequestResponse<T>();
            requestResponse.Status = response.Status;
            requestResponse.Exception = response.Exception;

            return requestResponse;
        }
    }

    public enum RequestStatus
    {
        Fail,
        Success
    }
}

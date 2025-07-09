// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Runtime;
    using System;

    public class ResponseDeserializationException : Exception
    {
        public Result ErrorCode;

        public ResponseDeserializationException(Result code, string message) : base(message)
        {
            ErrorCode = code;
        }
    }
}


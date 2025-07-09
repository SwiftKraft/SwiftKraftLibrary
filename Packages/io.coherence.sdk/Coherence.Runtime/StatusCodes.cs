// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime
{
    using System.Net;

    /// <summary>
    /// Utility class for functionality related to <see cref="HttpStatusCode">HTTP response status codes</see>
    /// in integer form.
    /// </summary>
    /// <seealso cref="RequestException.StatusCode"/>
    internal static class StatusCodes
    {
        public static bool IsSuccess(int statusCode)
            // https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpresponsemessage.issuccessstatuscode
            =>  statusCode is >= 200 and <= 299;
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Coherence.Connection;

    /// <summary>
    /// Request from another client for authority over an entity.
    /// </summary>
    public struct AuthorityRequest
    {
        /// <summary>
        /// The ClientID of the client requesting authority.
        /// </summary>
        public ClientID RequesterID { get; }

        /// <summary>
        /// The type of authority requested.
        /// </summary>
        public AuthorityType AuthorityType { get; }

        private AuthorityRequestRespond respondDelegate;

        internal AuthorityRequest(ClientID requester, AuthorityType authorityType, AuthorityRequestRespond respond)
        {
            RequesterID = requester;
            AuthorityType = authorityType;
            respondDelegate = respond;
        }

        /// <summary>
        /// Responds to the entity authority request.
        /// </summary>
        public void Respond(AuthorityRequestResponse response)
        {
            respondDelegate(response);
        }

        /// <inheritdoc cref="Respond(AuthorityRequestResponse)"/>
        /// <param name="accept">Boolean stating if the entity authority request should be accepted.</param>
        public void Respond(bool accept)
        {
            Respond(new AuthorityRequestResponse(accept));
        }

        /// <summary>
        /// Responds to the entity authority request with an acceptance.
        /// </summary>
        public void Accept()
        {
            Respond(new AuthorityRequestResponse(true));
        }

        /// <summary>
        /// Responds to the entity authority request with a rejection.
        /// </summary>
        public void Reject()
        {
            Respond(new AuthorityRequestResponse(false));
        }
    }

    /// <summary>
    /// Response to a request for authority over an entity.
    /// </summary>
    public struct AuthorityRequestResponse
    {
        public bool Accepted { get; }

        /// <param name="accepted">Boolean stating if the entity authority request should be accepted.</param>
        public AuthorityRequestResponse(bool accepted)
        {
            Accepted = accepted;
        }
    }

    internal delegate void AuthorityRequestRespond(AuthorityRequestResponse response);
}

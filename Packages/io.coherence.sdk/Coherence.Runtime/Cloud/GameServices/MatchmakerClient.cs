// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using Log;
    using Runtime;

    public class MatchMakerException : System.Exception
    {
        public Result ErrorCode;

        public MatchMakerException(Result code, string message) : base(message)
        {
            ErrorCode = code;
        }
    }

    public class MatchmakerClient
    {
        private IRequestFactory requestFactory;
        private IAuthClientInternal authClient;

        public MatchmakerClient(RequestFactory requestFactory, AuthClient authClient)
        {
            this.requestFactory = requestFactory;
            this.authClient = authClient;
        }

        internal MatchmakerClient(IRequestFactory requestFactory, IAuthClientInternal authClient)
        {
            this.requestFactory = requestFactory;
            this.authClient = authClient;
        }

        public async Task<MatchResponse> Match(string region, string team, string payload, string[] tags, string[] friends)
        {
            if (!authClient.LoggedIn)
            {
                throw new MatchMakerException(Result.InvalidCredentials, "");
            }

            MatchRequest request = new MatchRequest
            {
                Region = region,
                Team = team,
                Score = 0,
                Payload = payload,
                Friends = friends,
                Tags = tags
            };

            string body = Utils.CoherenceJson.SerializeObject(request);
            try
            {
                var text = await requestFactory.SendRequestAsync("/match", "POST", body, null,
                    $"{nameof(MatchmakerClient)}.{nameof(Match)}", authClient.SessionToken);

                MatchResponse match = Utils.CoherenceJson.DeserializeObject<MatchResponse>(text);

                return match;
            }
            catch (RequestException ex)
            {
                throw new MatchMakerException(ResultUtils.ErrorToResult(ex.ErrorCode) ?? Result.ServerError, "");
            }
        }
    }
}

namespace Coherence.Cloud
{
    using Runtime;
    using System;
    using System.Collections.Generic;
    using Log;
    using System.Threading.Tasks;

    public class RoomRegionsService
    {
        private IRequestFactory requestFactory;

        private IAuthClientInternal authClient;
        private readonly Logger logger = Log.GetLogger<RoomRegionsService>();

        private readonly string regionEndpoint = "/rooms/regions";

        private List<string> regions = new();

        private readonly List<Action<RequestResponse<IReadOnlyList<string>>>> fetchRegionsCallbackList = new();
        private bool isFetchingRegionsAsync;

        public IReadOnlyList<string> Regions => regions;

        public RoomRegionsService(RequestFactory requestFactory, AuthClient authClient) : this(requestFactory, (IAuthClientInternal)authClient) { }

        internal RoomRegionsService(IRequestFactory requestFactory, IAuthClientInternal authClient)
        {
            this.requestFactory = requestFactory;
            this.authClient = authClient;
        }

        public TimeSpan GetFetchRegionsCooldown()
        {
            return requestFactory.GetRequestCooldown(regionEndpoint, "GET");
        }

        public void FetchRegions(Action<RequestResponse<IReadOnlyList<string>>> onRequestFinished)
        {
            fetchRegionsCallbackList.Add(onRequestFinished);

            if (fetchRegionsCallbackList.Count > 1)
            {
                return;
            }

            logger.Trace("FetchRegions - start");

            regions.Clear();

            requestFactory.SendRequest(regionEndpoint, "GET", null, null, $"{nameof(RoomRegionsService)}.{nameof(FetchRegions)}", authClient.SessionToken,
                response =>
                {
                    var requestResponse = RequestResponse<IReadOnlyList<string>>.GetRequestResponse(response);

                    if (requestResponse.Status == RequestStatus.Fail)
                    {
                        requestResponse.Result = new List<string>();
                        foreach (var callback in fetchRegionsCallbackList)
                        {
                            callback?.Invoke(requestResponse);
                        }
                        fetchRegionsCallbackList.Clear();
                        return;
                    }

                    string[] regionList = Array.Empty<string>();

                    try
                    {
                        regionList = Utils.CoherenceJson.DeserializeObject<RegionFetchResponse>(response.Result).Regions;

                        logger.Trace("FetchRegions - end", ("regions count", regionList.Length));
                    }
                    catch (Exception exception)
                    {
                        requestResponse.Status = RequestStatus.Fail;
                        requestResponse.Exception = new ResponseDeserializationException(Result.InvalidResponse, exception.Message);

                        logger.Error(Error.RuntimeCloudDeserializationException,
                            ("Request", nameof(FetchRegions)),
                            ("Response", response.Result),
                            ("exception", exception));
                    }
                    finally
                    {
                        regions.AddRange(regionList);
                        requestResponse.Result = regionList;

                        IterateCallbackList(requestResponse);
                    }
                });
        }

        public async Task<IReadOnlyList<string>> FetchRegionsAsync()
        {
            logger.Trace("FetchRegions - start");

            while (isFetchingRegionsAsync)
            {
                await Task.Yield();
            }

            isFetchingRegionsAsync = true;

            regions.Clear();

            var text = await requestFactory.SendRequestAsync(regionEndpoint, "GET", null, null, $"{nameof(RoomRegionsService)}.{nameof(FetchRegionsAsync)}", authClient.SessionToken);

            string[] regionList = Array.Empty<string>();

            try
            {
                regionList = Utils.CoherenceJson.DeserializeObject<RegionFetchResponse>(text).Regions;

                logger.Trace("FetchRegions - end", ("regions count", regionList.Length));
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeCloudDeserializationException,
                    ("Request", nameof(FetchRegionsAsync)),
                    ("Response", text),
                    ("exception", exception));

                throw new ResponseDeserializationException(Result.InvalidResponse, exception.Message);
            }
            finally
            {
                regions.AddRange(regionList);

                isFetchingRegionsAsync = false;
            }

            return regions;
        }

        private void IterateCallbackList(RequestResponse<IReadOnlyList<string>> response)
        {
            foreach (var callback in fetchRegionsCallbackList)
            {
                callback?.Invoke(response);
            }

            fetchRegionsCallbackList.Clear();
        }
    }

}

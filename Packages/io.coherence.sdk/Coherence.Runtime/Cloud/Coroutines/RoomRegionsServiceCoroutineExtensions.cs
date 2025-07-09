#if UNITY_5_3_OR_NEWER

namespace Coherence.Cloud.Coroutines
{
    using Coherence.Cloud;
    using System.Collections.Generic;

    public static class RoomRegionsServiceCoroutineExtensions
    {
        public static WaitForRequestResponse<IReadOnlyList<string>> WaitForFetchRegions(this RoomRegionsService roomRegionsService)
        {
            return new WaitForRequestResponse<IReadOnlyList<string>>((fn) =>
            {
                roomRegionsService.FetchRegions(fn);
            });
        }
    }
}

#endif

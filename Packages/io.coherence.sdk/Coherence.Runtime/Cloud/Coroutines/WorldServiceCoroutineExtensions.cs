#if UNITY_5_3_OR_NEWER

namespace Coherence.Cloud.Coroutines
{
    using Coherence.Cloud;
    using System.Collections.Generic;

    public static class WorldServiceCoroutineExtensions
    {
        public static WaitForPredicate WaitForLogin(this WorldsService worldsService)
        {
            return new WaitForPredicate(() => worldsService.IsLoggedIn);
        }

        public static WaitForRequestResponse<IReadOnlyList<WorldData>> WaitForFetchWorlds(this WorldsService worldsService, string region = "", string simSlug = "")
        {
            return new WaitForRequestResponse<IReadOnlyList<WorldData>>((fn) =>
            {
                worldsService.FetchWorlds(fn, region, simSlug);
            });
        }
    }
}

#endif

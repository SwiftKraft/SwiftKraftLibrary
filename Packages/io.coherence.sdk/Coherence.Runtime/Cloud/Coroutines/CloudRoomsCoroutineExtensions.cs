#if UNITY_5_3_OR_NEWER

namespace Coherence.Cloud.Coroutines
{
    using Coherence.Cloud;
    using System.Collections.Generic;

    public static class CloudRoomsCoroutineExtensions
    {
        public static WaitForPredicate WaitForCloudConnection(this CloudRooms cloudService)
        {
            return new WaitForPredicate(() => cloudService.IsConnectedToCloud);
        }

        public static WaitForPredicate WaitForLogin(this CloudRooms cloudRooms)
        {
            return new WaitForPredicate(() => cloudRooms.IsLoggedIn);
        }

        public static WaitForRequestResponse<IReadOnlyList<string>> WaitForFetchRegions(this CloudRooms cloudRooms)

        {
            return new WaitForRequestResponse<IReadOnlyList<string>>((fn) =>
            {
                cloudRooms.RefreshRegions(fn);
            });
        }
    }
}

#endif

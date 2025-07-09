#if UNITY_5_3_OR_NEWER

namespace Coherence.Cloud.Coroutines
{
    using Coherence.Cloud;

    public static class CloudServiceCoroutineExtensions
    {
        public static WaitForPredicate WaitForCloudConnection(this CloudService cloudService)
        {
            return new WaitForPredicate(() => cloudService.IsConnectedToCloud);
        }

        public static WaitForPredicate WaitForLogin(this CloudService cloudService)
        {
            return new WaitForPredicate(() => cloudService.IsLoggedIn);
        }
    }
}

#endif

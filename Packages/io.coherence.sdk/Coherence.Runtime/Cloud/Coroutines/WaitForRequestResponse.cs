#if UNITY_5_3_OR_NEWER

namespace Coherence.Cloud.Coroutines
{
    using UnityEngine;
    using Coherence.Cloud;
    using Log;
    using Logger = Log.Logger;
    using System;

    public class WaitForRequestResponse<TResult> : CustomYieldInstruction
    {
        public Logger logger;

        public RequestResponse<TResult> RequestResponse { get; private set; }

        private bool done;

        public override bool keepWaiting => !done;

        private void OnComplete(RequestResponse<TResult> requestResponse)
        {
            RequestResponse = requestResponse;
            done = true;
        }

        public WaitForRequestResponse(Action<Action<RequestResponse<TResult>>> fn)
        {
            if (fn == null)
            {
                if (logger == null)
                {
                    logger = Log.GetLogger<WaitForRequestResponse<TResult>>();
                }

                logger.Error(Error.RuntimeCloudRequestActionNull);

                done = true;
                return;
            }

            fn.Invoke(OnComplete);
        }
    }
}

#endif

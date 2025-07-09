#if UNITY_5_3_OR_NEWER

namespace Coherence.Cloud.Coroutines
{
    using UnityEngine;
    using System;

    public class WaitForPredicate : CustomYieldInstruction
    {
        private Func<bool> predicate;

        public override bool keepWaiting => predicate != null && !predicate.Invoke();

        public WaitForPredicate(Func<bool> predicate)
        {
            this.predicate = predicate;
        }
    }
}

#endif

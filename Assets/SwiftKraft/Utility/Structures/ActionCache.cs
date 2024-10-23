using System;

namespace SwiftKraft.Utils
{
    public class ActionCache<T> : CacheBase<T>
    {
        public readonly Action<ActionCache<T>> RefreshAction;

        public ActionCache(Action<ActionCache<T>> refreshAction) => RefreshAction = refreshAction;

        public override void Refresh() => RefreshAction?.Invoke(this);
    }
}

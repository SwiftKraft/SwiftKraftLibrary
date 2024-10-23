namespace SwiftKraft.Utils
{
    public abstract class CacheBase<T>
    {
        public T Cache
        {
            get
            {
                if (_cached == null)
                    Refresh();
                return _cached;
            }
            set => _cached = value;
        }

        T _cached;

        public abstract void Refresh();
    }
}

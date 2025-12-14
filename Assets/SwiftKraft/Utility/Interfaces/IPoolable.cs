namespace SwiftKraft.Utils
{
    public interface IPoolable : IResettable
    {
        public bool Vacant { get; }

        /// <summary>
        /// Called when the object is ordered to spawn from the pool, this is where you can set the vacancy to false.
        /// </summary>
        public void Spawn();
        /// <summary>
        /// Called when the object is ordered to recycle back into the pool, this is where you can set the vacancy to true.
        /// </summary>
        public void Recycle();
        /// <summary>
        /// Called when the object is added to a pool for the first time.
        /// </summary>
        public void Pool();
        /// <summary>
        /// Called when the object is permanently removed from the pool.
        /// </summary>
        public void Delete();
    }
}

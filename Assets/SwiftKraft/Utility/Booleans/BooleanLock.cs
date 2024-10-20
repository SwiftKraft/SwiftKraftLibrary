using System;
using System.Collections.Generic;

namespace SwiftKraft.Utils
{
    /// <summary>
    /// A fancy boolean that can be set to false by adding locks instead of changing a raw value and causing conflicts.
    /// </summary>
    [Serializable]
    public class BooleanLock
    {
        public readonly List<Lock> Locks = new();

        public bool Inverse;

        public bool Get()
        {
            foreach (Lock lk in Locks)
                if (lk.Active)
                    return Inverse;
            return !Inverse;
        }

        /// <summary>
        /// Adds a lock.
        /// </summary>
        /// <returns>Reference to the lock.</returns>
        public Lock AddLock()
        {
            Lock l = new();
            Locks.Add(l);
            return l;
        }

        /// <summary>
        /// Removes all locks.
        /// </summary>
        public void ClearLocks() => Locks.Clear();

        /// <summary>
        /// Removes a certain lock.
        /// </summary>
        /// <param name="l">The lock reference.</param>
        public void RemoveLock(Lock l) => Locks.Remove(l);

        public static implicit operator bool(BooleanLock boolLock) => boolLock.Get();

        /// <summary>
        /// A C# object that holds a single boolean and an event.
        /// </summary>
        [Serializable]
        public class Lock
        {
            public bool Active
            {
                get => _active;
                set
                {
                    if (_active == value)
                        return;

                    _active = value;
                    OnChanged?.Invoke();
                }
            }
            bool _active;

            public event Action OnChanged;

            public static implicit operator bool(Lock lck) => lck.Active;
        }
    }
}

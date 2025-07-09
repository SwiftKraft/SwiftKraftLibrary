// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling.Modules
{
    using System;
    using System.Collections.Generic;

    internal class GuardModule<T> : IPoolModule<T>
    {
        private readonly bool threadSafe;
        private readonly bool enabled;
        private readonly HashSet<T> rented = new();

        public GuardModule(bool threadSafe = false, bool enabled = true)
        {
            this.threadSafe = threadSafe;
            this.enabled = enabled;
        }

        public void OnRent(in T item)
        {
            if (!enabled)
            {
                return;
            }

            if (!threadSafe)
            {
                VerifyRentedOnce(item);
                return;
            }

            lock (rented)
            {
                VerifyRentedOnce(item);
            }
        }

        public void OnReturn(in T item)
        {
            if (!enabled)
            {
                return;
            }

            if (!threadSafe)
            {
                VerifyReturnedWasRented(item);
                return;
            }

            lock (rented)
            {
                VerifyReturnedWasRented(item);
            }
        }

        private void VerifyRentedOnce(in T item)
        {
            if (rented.Add(item))
            {
                return;
            }

            throw new Exception($"Pooled object of type {typeof(T)} was rented twice: {item}");
        }

        private void VerifyReturnedWasRented(in T item)
        {
            if (rented.Remove(item))
            {
                return;
            }

            throw new Exception($"Pooled object of type {typeof(T)} was returned without being rented: {item}");
        }
    }
}

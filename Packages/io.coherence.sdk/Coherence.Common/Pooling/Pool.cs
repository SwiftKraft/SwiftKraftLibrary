// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling
{
    using System;
    using System.Collections.Generic;
    using Modules;
    using Storage;

    internal partial class Pool<T> : IPool<T>
    {
        internal const int DefaultPrefillSize = 32;

        private readonly List<IPoolModule<T>> modules;
        private readonly IPoolStorage<T> storage;
        private readonly Func<IPool<T>, T> objectGenerator;

        public static PoolBuilder Builder(Func<IPool<T>, T> objectGenerator) => new(objectGenerator);

        protected Pool(
            Func<IPool<T>, T> objectGenerator,
            IPoolStorage<T> storage = null,
            IEnumerable<IPoolModule<T>> modules = null,
            int prefillSize = DefaultPrefillSize)
        {
            this.objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            this.storage = storage ?? new StackStorage<T>();
            this.modules = modules != null ? new List<IPoolModule<T>>(modules) : null;

            Prefill(prefillSize);
        }

        private void Prefill(int prefillSize)
        {
            for (var i = 0; i < prefillSize; i++)
            {
                storage.Add(GenerateObject());
            }
        }

        public T Rent()
        {
            var rented = storage.TryTake(out var item) ? item : GenerateObject();
            ExecuteModulesOnRent(rented);
            return rented;
        }

        public void Return(T item)
        {
            ExecuteModulesOnReturn(item);
            storage.Add(item);
        }

        protected void AddModule(IPoolModule<T> module)
        {
            modules.Add(module);
        }

        private T GenerateObject()
        {
            return objectGenerator(this);
        }

        private void ExecuteModulesOnRent(in T item)
        {
            if (modules == null)
            {
                return;
            }

            foreach (var mod in modules)
            {
                mod.OnRent(item);
            }
        }

        private void ExecuteModulesOnReturn(in T item)
        {
            if (modules == null)
            {
                return;
            }

            foreach (var mod in modules)
            {
                mod.OnReturn(item);
            }
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Modules;
    using Storage;

    internal static class BuilderExtensions
    {
        public static Pool<T>.PoolBuilder WithReusables<T>(this Pool<T>.PoolBuilder builder)
            where T : IReusable
        {
            builder.WithModule(new ActionsModule<T>().WithReturnAction(item => item.ResetState()));
            return builder;
        }
    }

    internal partial class Pool<T>
    {
        internal class PoolBuilder
        {
            private readonly List<IPoolModule<T>> modules = new();
            private IPoolStorage<T> storage;
            private readonly Func<IPool<T>, T> objectGenerator;
            private bool useGuard = !typeof(T).IsValueType;
            private bool isConcurrent;
            private bool built;
            private int prefillSize = DefaultPrefillSize;
            private ActionsModule<T> actionsModule;

            public PoolBuilder(Func<IPool<T>, T> objectGenerator)
            {
                this.objectGenerator = objectGenerator;
            }

            public PoolBuilder Prefill(int prefillSize)
            {
                 this.prefillSize = prefillSize;
                 return this;
            }

            public PoolBuilder WithReturnAction(Action<T> action)
            {
                actionsModule ??= new ActionsModule<T>();
                actionsModule.WithReturnAction(action);
                return this;
            }

            public PoolBuilder WithRentAction(Action<T> action)
            {
                actionsModule ??= new ActionsModule<T>();
                actionsModule.WithRentAction(action);
                return this;
            }

            public PoolBuilder WithModule(IPoolModule<T> module)
            {
                modules.Add(module);
                return this;
            }

            public PoolBuilder Concurrent()
            {
                isConcurrent = true;
                storage = new ConcurrentStorage<T>();
                return this;
            }

            public PoolBuilder WithNoGuard()
            {
                useGuard = false;
                return this;
            }

            public Pool<T> Build()
            {
                built = built
                    ? throw new InvalidOperationException("Pool has already been built.")
                    : true;

                AddGuardModule();
                AddActionsModule();

                storage ??= new StackStorage<T>();

                return new Pool<T>(objectGenerator, storage, modules, prefillSize);
            }

            private void AddActionsModule()
            {
                if (actionsModule != null)
                {
                    modules.Add(actionsModule);
                }
            }

            [Conditional("DEBUG")]
            private void AddGuardModule()
            {
                if (useGuard)
                {
                    modules.Add(new GuardModule<T>(isConcurrent, true));
                }
            }
        }
    }
}

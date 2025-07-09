// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections.Generic;

    public class CoherenceObjectPool<T> : IDisposable where T : class
    {
        private readonly Stack<T> objectStack;
        private readonly Func<T> createFunc;
        private readonly Func<T, bool> onGet;
        private readonly Action<T> onRelease;
        private readonly Action<T> onDestroy;
        private readonly int maxSize;
        private bool collectionCheck;

        public int CountAll { get; private set; }

        public int CountActive => CountAll - CountInactive;

        public int CountInactive => objectStack.Count;

        public CoherenceObjectPool(
          Func<T> createFunc,
          Func<T, bool> actionOnGet = null,
          Action<T> actionOnRelease = null,
          Action<T> actionOnDestroy = null,
          bool collectionCheck = true,
          int maxSize = 10000)
        {
            if (maxSize <= 0)
            {
                throw new ArgumentException("Max Size must be greater than 0", nameof (maxSize));
            }

            this.objectStack = new Stack<T>();
            this.createFunc = createFunc ?? throw new ArgumentNullException(nameof (createFunc));
            this.maxSize = maxSize;
            this.onGet = actionOnGet;
            this.onRelease = actionOnRelease;
            this.onDestroy = actionOnDestroy;
            this.collectionCheck = collectionCheck;
        }

        public void ForceGet(T instance)
        {
            if (objectStack.Contains(instance))
            {
                var queue = new Queue<T>();
                T poppedInstance = null;

                while (poppedInstance != instance)
                {
                    poppedInstance = objectStack.Pop();

                    if (poppedInstance != instance)
                    {
                        queue.Enqueue(poppedInstance);
                    }
                }

                while (queue.Count > 0)
                {
                    objectStack.Push(queue.Dequeue());
                }
            }
        }

        public T Get()
        {
            T obj = null;
            bool validObject = false;

            while (!validObject)
            {
                obj = GetObjectFromStackOrCreate();

                if (obj == null)
                {
                    continue;
                }

                validObject = onGet.Invoke(obj);

                if (!validObject)
                {
                    CountAll--;
                }
            }

            return obj;
        }

        public void Release(T element)
        {
            if (collectionCheck && objectStack.Count > 0 && objectStack.Contains(element))
            {
                throw new InvalidOperationException("Trying to release an object that has already been released to the pool.");
            }

            onRelease?.Invoke(element);

            if (CountInactive < maxSize)
            {
                objectStack.Push(element);
            }
            else
            {
                onDestroy?.Invoke(element);
            }
        }

        public void Clear()
        {
            if (onDestroy != null)
            {
                foreach (T obj in objectStack)
                {
                    onDestroy(obj);
                }
            }

            objectStack.Clear();
            CountAll = 0;
        }

        public void Dispose() => this.Clear();

        private T GetObjectFromStackOrCreate()
        {
            T obj;
            if (objectStack.Count == 0)
            {
                obj = createFunc();
                CountAll++;
            }
            else
            {
                obj = objectStack.Pop();
            }

            return obj;
        }
    }
}

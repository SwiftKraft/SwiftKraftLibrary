using System.Collections.Generic;

namespace SwiftKraft.Utils
{
    public class Blackboard<T>
    {
        public readonly Dictionary<string, T> Values = new();

        public bool Remove(string name) => Values.Remove(name);

        public bool TryAdd(string name, T val)
        {
            if (Values.ContainsKey(name))
                return false;
            Values.Add(name, val);
            return true;
        }

        public bool TryAdd<E>(string name, out E res) where E : T, new()
        {
            res = default;
            if (Values.ContainsKey(name))
                return false;

            res = new E();
            Values.Add(name, res);
            return true;
        }

        public E Add<E>(string name) where E : T, new()
        {
            TryAdd(name, out E res);
            return res;
        }

        public bool TryGet(string name, out T res)
        {
            res = default;
            if (Values.TryGetValue(name, out T val))
            {
                res = val;
                return true;
            }
            return false;
        }

        public T Get(string name)
        {
            TryGet(name, out T t);
            return t;
        }

        public bool TryGet<E>(string name, out E res) where E : T
        {
            res = default;
            if (TryGet(name, out T t) && t is E e)
            {
                res = e;
                return true;
            }
            return false;
        }

        public E Get<E>(string name) where E : T
        {
            T t = Get(name);
            return t is E e ? e : default;
        }
    }

    public class Blackboard : Blackboard<object> { }
}

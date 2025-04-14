using System.Collections.Generic;
using System.Linq;

namespace SwiftKraft.Utils
{
    public class SafeDictionary<ID, V> : Dictionary<ID, V>
    {
        public bool TryGet<T>(out T value) where T : V
        {
            value = Get<T>(); 
            return value != null;
        }

        public bool TryGet<T>(ID id, out T value) where T : V
        {
            value = Get<T>(id);
            return value != null;
        }

        public T Get<T>() where T : V => Values.OfType<T>().FirstOrDefault();

        public T Get<T>(ID id) where T : V => ContainsKey(id) && this[id] is T t ? t : default;

        public new bool Add(ID id, V reference)
        {
            if (ContainsKey(id))
                return false;

            base.Add(id, reference);
            return true;
        }

        public bool Add<T>(ID id, out V reference) where T : V, new()
        {
            reference = new T();
            return Add(id, reference);
        }
    }
}

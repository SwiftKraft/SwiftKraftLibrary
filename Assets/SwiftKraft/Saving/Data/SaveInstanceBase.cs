using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Saving.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SaveInstanceBase<E> where E : SaveDataBase
    {
        public class ExposedVariable
        {
            public readonly string ID;
            public readonly Func<object> Get;
            public readonly Type Type;

            public ExposedVariable(string id, Func<object> get, Type type)
            {
                ID = id;
                Get = get;
                Type = type;
            }
        }

        public bool Disposed { get; protected set; }

        [JsonProperty("data")]
        public Dictionary<string, SaveDataBase> Data { get; private set; } = new();
        public readonly Dictionary<string, ExposedVariable> Exposed = new(); 

        public virtual void InitializeData<T>(T t) where T : E { }

        public bool ExposeVariable(string id, Func<object> func, Type type)
        {
            if (Exposed.ContainsKey(id))
                return false;

            Exposed.Add(id, new(id, func, type));
            return true;
        }

        public bool ExposeVariable<T>(string id, Func<object> func) => ExposeVariable(id, func, typeof(T));

        public T AddData<T>(string id, Action<T> initAction = null) where T : E, new()
        {
            if (Data.ContainsKey(id))
                return null;

            T t = new();
            InitializeData(t);
            initAction?.Invoke(t);
            Data.Add(id, t);
            return t;
        }

        public bool TryAddData<T>(string id, out T dat, Action<T> initAction = null) where T : E, new()
        {
            dat = AddData<T>(id, initAction);
            return dat != null;
        }

        public T GetData<T>(string id) where T : E => Data.ContainsKey(id) && Data[id] is T t ? t : null;

        public bool TryGetData<T>(string id, out T dat) where T : E
        {
            dat = GetData<T>(id);
            return dat != null;
        }

        public bool TryData<T>(string id, out T dat, Action<T> initAction = null) where T : E, new() => TryGetData(id, out dat) || TryAddData(id, out dat, initAction);

        public bool RemoveData(string id) => Data.Remove(id);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SaveDataBase { }
}

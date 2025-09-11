using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Saving.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SaveInstanceBase<E> where E : SaveDataBase
    {
        public bool Disposed { get; protected set; }

        [JsonProperty("data")]
        public Dictionary<string, SaveDataBase> Data { get; private set; } = new();

        public virtual void InitializeData<T>(T t) where T : E { }

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

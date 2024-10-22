using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Saving.Progress
{
    [JsonObject(MemberSerialization.OptIn, IsReference = false)]
    public class ProgressSave
    {
        [JsonProperty]
        public string Name;

        [JsonProperty]
        public Dictionary<string, Progressable> Progress = new();

        public bool TryAddProgressable<T>(string id, out T progress) where T : Progressable, new()
        {
            progress = AddProgressable<T>(id);
            return progress != null;
        }

        public T AddProgressable<T>(string id) where T : Progressable, new()
        {
            if (Progress.ContainsKey(id))
                return null;

            T t = new() { ID = id };
            Progress.Add(id, t);
            return t;
        }

        public bool TryGetProgressable<T>(string id, out T progress) where T : Progressable
        {
            progress = GetProgressable<T>(id);
            return progress != null;
        }

        public T GetProgressable<T>(string id) where T : Progressable => Progress.ContainsKey(id) && Progress[id] is T t ? t : null;

        public void ResetProgress() => Progress.Clear();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Progressable
    {
        [JsonProperty]
        public string ID { get; set; }

        public event Action OnUpdate;
    }
}

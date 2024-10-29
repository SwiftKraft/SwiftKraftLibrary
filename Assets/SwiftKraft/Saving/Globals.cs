using Newtonsoft.Json;
using System.Collections.Generic;

namespace SwiftKraft.Saving
{
    [JsonObject(MemberSerialization.OptIn, IsReference = false)]
    public class Globals
    {
        [JsonProperty]
        public Dictionary<string, GlobalElement> Elements = new();

        public bool TryGetElement<T>(string id, out T element) where T : GlobalElement
        {
            element = GetElement<T>(id);
            return element != null;
        }

        public T GetElement<T>(string id) where T : GlobalElement => !Elements.ContainsKey(id) || Elements[id] is not T t ? null : t;

        public bool TryAddElement<T>(string id, out T element) where T : GlobalElement, new()
        {
            element = AddElement<T>(id);
            return element != null;
        }

        public T AddElement<T>(string id) where T : GlobalElement, new()
        {
            if (Elements.ContainsKey(id))
                return null;

            T t = new() { ID = id };
            Elements.Add(id, t);
            return t;
        }

        public bool TryElement<T>(string id, out T element) where T : GlobalElement, new() => TryGetElement(id, out element) || TryAddElement(id, out element);
    }

    [JsonObject(MemberSerialization.OptIn, IsReference = false)]
    public abstract class GlobalElement
    {
        [JsonProperty]
        public string ID;
    }
}

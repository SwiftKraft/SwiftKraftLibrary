using Newtonsoft.Json;
using SwiftKraft.Utils;

namespace SwiftKraft.Gameplay.Building
{
    [JsonObject(MemberSerialization.OptIn)]
    public class BuildInstance : SaveInstanceBase<BuildDataBase>
    {
        [JsonProperty]
        public readonly string PrefabID;
        [JsonProperty]
        public TransformData Transform;
    }

    public class BuildDataBase : SaveDataBase { }
}
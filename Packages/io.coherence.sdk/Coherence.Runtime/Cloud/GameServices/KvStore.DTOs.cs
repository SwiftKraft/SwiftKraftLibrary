// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    public partial class KvStoreClient
    {
        private enum DataOperationType
        {
            [EnumMember(Value = "set")]
            Set,

            [EnumMember(Value = "del")]
            Delete
        }

#pragma warning disable 0649
        private struct DataSyncItem
        {
            [JsonProperty("key")]
            public string Key;

            [JsonProperty("val")]
            public string Value;

            [JsonProperty("op")]
            [JsonConverter(typeof(StringEnumConverter))]
            public DataOperationType Operation;

            [JsonIgnore]
            public bool Dirty;
        }

        private struct DataSync
        {
            [JsonProperty("kv")]
            public List<DataSyncItem> Data;
        }
#pragma warning disable 0649
    }
}

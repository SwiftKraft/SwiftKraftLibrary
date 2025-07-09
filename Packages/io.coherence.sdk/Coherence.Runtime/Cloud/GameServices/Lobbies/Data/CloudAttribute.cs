// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System;

    public struct CloudAttribute
    {
        [JsonIgnore]
        public string Key => key;

        [JsonProperty("key")]
        private string key;

        [JsonProperty("val")]
        private object value;

        [JsonProperty("pub")]
        private bool? isPublic;

        [JsonProperty("idx")]
        private string index;

        [JsonProperty("aggr")]
        private string aggregate;

        public CloudAttribute(string key, long value, bool? isPublic = null)
        {
            this.key = key;
            this.value = value;
            this.isPublic = isPublic;
            this.index = null;
            this.aggregate = null;
        }

        public CloudAttribute(string key, string value, bool? isPublic = null)
        {
            this.key = key;
            this.value = value;
            this.isPublic = isPublic;
            this.index = null;
            this.aggregate = null;
        }

        public CloudAttribute(string key, long value, IntAttributeIndex index, IntAggregator aggregate, bool? isPublic = null)
        {
            this.key = key;
            this.value = value;
            this.isPublic = isPublic;
            this.index = index.ToString();
            this.aggregate = aggregate == IntAggregator.None ? null : aggregate.ToString().ToLowerInvariant();
        }

        public CloudAttribute(string key, string value, StringAttributeIndex index, StringAggregator aggregate, bool? isPublic = null)
        {
            this.key = key;
            this.value = value;
            this.isPublic = isPublic;
            this.index = index.ToString();
            this.aggregate = aggregate == StringAggregator.None ? null : aggregate.ToString().ToLowerInvariant();
        }

        public long GetLongValue()
        {
            try
            {
                return (long)value;
            }
            catch (InvalidCastException)
            {
                LogError($"Invalid Cast: Attribute {key} is not a long value.");
                return 0;
            }
        }

        public string GetStringValue()
        {
            try
            {
                return (string)value;
            }
            catch (InvalidCastException)
            {
                LogError($"Invalid Cast: Attribute {key} is not a string value.");
                return string.Empty;
            }
        }
        
        private void LogError(string errorMsg)
        {
#if UNITY
            UnityEngine.Debug.LogError(errorMsg);
#else
            Console.WriteLine(errorMsg);
#endif
        }
    }

    public enum StringAttributeIndex
    {
        s1,
        s2,
        s3,
        s4,
        s5
    }

    public enum IntAttributeIndex
    {
        n1,
        n2,
        n3,
        n4,
        n5
    }

    public enum IntAggregator
    {
        None,
        Sum,
        Avg,
        Min,
        Max,
        Owner
    }

    public enum StringAggregator
    {
        None,
        Owner
    }
}

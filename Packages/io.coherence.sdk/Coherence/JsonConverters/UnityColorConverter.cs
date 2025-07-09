// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

#if UNITY
namespace Coherence.Toolkit
{
    using Newtonsoft.Json;
    using System;
    using UnityEngine;

    public sealed class UnityColorConverter : JsonConverter
    {
        static readonly char[] Separator = {','};

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is Color color))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue($"{color.r},{color.g},{color.b},{color.a}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException($"Invalid token type. Expected: {JsonToken.String}, Was: {reader.TokenType}");
            }

            var value = reader.Value.ToString();

            try
            {
                var rgba = value.Split(Separator);
                return new Color(float.Parse(rgba[0]), float.Parse(rgba[1]), float.Parse(rgba[2]), float.Parse(rgba[3]));
            }
            catch (Exception exception)
            {
                throw new JsonSerializationException($"Failed to deserialize {nameof(Color)} from '{value}'", exception);
            }
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(Color);
    }
}
#endif

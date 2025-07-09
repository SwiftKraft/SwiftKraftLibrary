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

    public class UnityVector3Converter : JsonConverter
    {
        private static readonly char[] Separator = { ',' };

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is Vector3 vec))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue($"{vec.x},{vec.y},{vec.z}");
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
                var xyz = value.Split(Separator);
                return new Vector3(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
            }
            catch (Exception exception)
            {
                throw new JsonSerializationException($"Failed to deserialize {nameof(Vector3)} from '{value}'", exception);
            }
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(Vector3);
    }
}
#endif

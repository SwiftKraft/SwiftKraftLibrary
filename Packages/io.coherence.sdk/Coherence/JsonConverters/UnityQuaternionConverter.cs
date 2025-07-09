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

    public class UnityQuaternionConverter : JsonConverter
    {
        static readonly char[] Separator = {','};

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is Quaternion q))
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue($"{q.x},{q.y},{q.z},{q.w}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException(
                    $"Invalid token type. Expected: {JsonToken.String}, Was: {reader.TokenType}");
            }

            string value = reader.Value.ToString();

            try
            {
                string[] xyzw = value.Split(Separator);
                return new Quaternion(
                    float.Parse(xyzw[0]),
                    float.Parse(xyzw[1]),
                    float.Parse(xyzw[2]),
                    float.Parse(xyzw[3]));
            }
            catch (Exception exception)
            {
                throw new JsonSerializationException($"Failed to deserialize {nameof(Quaternion)} from '{value}'", exception);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(UnityEngine.Quaternion);
        }
    }
}
#endif

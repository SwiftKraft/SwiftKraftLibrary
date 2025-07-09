// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Log.Targets
{
    using System;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public static class JsonLogFormatter
    {
        private const string TIME_KEY = "time";
        private const string LEVEL_KEY = "level";
        private const string MESSAGE_KEY = "message";
        private const string LOGGER_KEY = "logger";

        private const string RFC3339_FORMAT = "yyyy-MM-dd'T'HH:mm:ss.fffK";

        [ThreadStatic] private static JsonSerializer serializer;
        [ThreadStatic] private static StringWriter stringWriter;
        [ThreadStatic] private static JsonTextWriter writer;

        public static string Format(LogLevel level, string message, (string key, object value)[] args, Type source)
        {
            serializer ??= JsonSerializer.CreateDefault(new JsonSerializerSettings()
            {
                ContractResolver = new LogJsonContractResolver()
            });

            stringWriter ??= new StringWriter();

            writer ??= new JsonTextWriter(stringWriter)
            {
                DateFormatString = RFC3339_FORMAT
            };

            stringWriter.GetStringBuilder().Clear();

            writer.WriteStartObject();

            writer.WritePropertyName(TIME_KEY);
            writer.WriteValue(DateTime.Now);

            writer.WritePropertyName(LEVEL_KEY);
            writer.WriteValue(level.ToString());

            writer.WritePropertyName(MESSAGE_KEY);
            writer.WriteValue(message);

            writer.WritePropertyName(LOGGER_KEY);
            writer.WriteValue(source.Name);

            foreach (var (key, value) in args)
            {
                var name = key;

                // if key is same as bulit-in keys, prepend underscore so names don't clash
                if (key == TIME_KEY || key == LEVEL_KEY || key == MESSAGE_KEY || key == LOGGER_KEY)
                {
                    name = "_" + name;
                }

                writer.WritePropertyName(name);
                serializer.Serialize(writer, value);
            }

            writer.WriteEndObject();

            writer.WriteWhitespace(Environment.NewLine);

            return stringWriter.ToString();
        }
    }

    public class LogJsonContractResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            JsonContract contract;

            // Mocked objects break DefaultContractResolver
            // this checks if an objectType is actually a Mock
            if (objectType.Namespace == "Castle.Proxies")
            {
                contract = new JsonObjectContract(objectType);
            }
            else
            {
                contract = base.CreateContract(objectType);
            }


            if (!objectType.IsPrimitive)
            {
                contract.Converter = new ToStringConverter(true);
            }

            return contract;
        }
    }

    internal class ToStringConverter : JsonConverter
    {
        // Needed to prevent default constructor that might be picked up by UnityConverterInitializer
        public ToStringConverter(bool isTrue) { }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(value.ToString());
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}

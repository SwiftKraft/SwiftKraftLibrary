using Newtonsoft.Json;

namespace SwiftKraft.Saving
{
    /// <summary>
    /// General manager for connecting with the Newtonsoft.JSON API.
    /// </summary>
    public static class NewtonsoftManager
    {
        /// <summary>
        /// Settings for the JSON Serializer for easier and consistent access.
        /// </summary>
        public static JsonSerializerSettings Settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
        };

        /// <summary>
        /// Converts an object into a JSON string.
        /// </summary>
        /// <param name="obj">The target object.</param>
        /// <returns>JSON string of the object.</returns>
        public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj, Formatting.Indented, Settings);

        /// <summary>
        /// Converts a JSON string into an object of provided type.
        /// </summary>
        /// <typeparam name="T">The type of the converted object.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <returns>Object of provided type</returns>
        public static T FromJson<T>(this string json) => JsonConvert.DeserializeObject<T>(json, Settings);
    }
}

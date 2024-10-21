using System.IO;
using UnityEngine;

namespace SwiftKraft.Saving
{
    /// <summary>
    /// A general manager class for saving and loading files at Application.persistentDataPath.
    /// </summary>
    public static class SaveManager
    {
        /// <summary>
        /// The path the files are saved to.
        /// </summary>
        public static string SavePath = Path.Combine(Application.persistentDataPath, "Saved");

        /// <summary>
        /// Converts an object into a JSON string and writes it into a file.
        /// </summary>
        /// <param name="obj">The target object.</param>
        /// <param name="name">The name of the file. (Excluding the .json extension)</param>
        /// <param name="folders">The folders leading up to the file location.</param>
        public static void Save(object obj, string name, params string[] folders)
        {
            string path = Path.Combine(SavePath, Path.Combine(folders));
            string pathFile = Path.Combine(path, name + ".json");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string json = obj.ToJson();
            File.WriteAllText(pathFile, json);
            Debug.Log("Saved File: " + pathFile);
        }

        /// <summary>
        /// Loads a JSON file and converts it into an object of provided type.
        /// </summary>
        /// <typeparam name="T">The type of object to convert into.</typeparam>
        /// <param name="name">The name of the file. (Excluding the .json extension)</param>
        /// <param name="folders">The folders leading up to the file location.</param>
        /// <returns>The converted object.</returns>
        public static T Load<T>(string name, params string[] folders)
        {
            string pathFile = Path.Combine(SavePath, Path.Combine(folders), name + ".json");

            if (!File.Exists(pathFile))
                return default;

            string json = File.ReadAllText(pathFile);
            return json.FromJson<T>();
        }

        /// <summary>
        /// Loads a JSON file and converts it into an object of provided type.
        /// </summary>
        /// <typeparam name="T">The type of object to convert into.</typeparam>
        /// <param name="result">The converted object.</param>
        /// <param name="name">The name of the file. (Excluding the .json extension)</param>
        /// <param name="folders">The folders leading up to the file location.</param>
        /// <returns>Whether the loading is successful or not.</returns>
        public static bool TryLoad<T>(out T result, string name, params string[] folders)
        {
            result = Load<T>(name, folders);
            return result != null;
        }
    }
}

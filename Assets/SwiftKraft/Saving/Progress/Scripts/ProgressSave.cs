using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SwiftKraft.Saving.Progress
{
    /// <summary>
    /// An object representing the saved game state.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn, IsReference = false)]
    public class ProgressSave
    {
        /// <summary>
        /// Name of the save file. (Excluding the .json extension)
        /// </summary>
        [JsonProperty]
        public string Name;

        /// <summary>
        /// The elements of the game that is to be saved.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, Progressable> Progress = new();

        /// <summary>
        /// Registers an element of the game to be saved.
        /// </summary>
        /// <typeparam name="T">A custom type containing the required data.</typeparam>
        /// <param name="id">The identifier of the element.</param>
        /// <param name="progress">The instantiated element object.</param>
        /// <returns>Whether it is successful or not.</returns>
        public bool TryAddProgressable<T>(string id, out T progress) where T : Progressable, new()
        {
            progress = AddProgressable<T>(id);
            return progress != null;
        }

        /// <summary>
        /// Registers an element of the game to be saved.
        /// </summary>
        /// <typeparam name="T">A custom type containing the required data.</typeparam>
        /// <param name="id">The identifier of the element.</param>
        /// <returns>The instantiated element object.</returns>
        public T AddProgressable<T>(string id) where T : Progressable, new()
        {
            if (Progress.ContainsKey(id))
                return null;

            T t = new() { ID = id };
            Progress.Add(id, t);
            return t;
        }

        /// <summary>
        /// Gets a saved element of the game based on identifier.
        /// </summary>
        /// <typeparam name="T">The custom type the data was saved in.</typeparam>
        /// <param name="id">The identifier of the element.</param>
        /// <param name="progress">The saved data object.</param>
        /// <returns>Whether it is successful or not.</returns>
        public bool TryGetProgressable<T>(string id, out T progress) where T : Progressable
        {
            progress = GetProgressable<T>(id);
            return progress != null;
        }

        /// <summary>
        /// Gets a saved element of the game based on identifier.
        /// </summary>
        /// <typeparam name="T">The custom type the data was saved in.</typeparam>
        /// <param name="id">The identifier of the element.</param>
        /// <returns>The saved data object.</returns>
        public T GetProgressable<T>(string id) where T : Progressable => Progress.ContainsKey(id) && Progress[id] is T t ? t : null;

        /// <summary>
        /// Clears all elements that are saved.
        /// </summary>
        public void ResetProgress() => Progress.Clear();
    }

    /// <summary>
    /// A base class for a game element that is to be saved.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Progressable
    {
        /// <summary>
        /// Identifier for the element.
        /// </summary>
        [JsonProperty]
        public string ID { get; set; }

        /// <summary>
        /// Called when the element updates.
        /// </summary>
        public event Action OnUpdate;
    }
}

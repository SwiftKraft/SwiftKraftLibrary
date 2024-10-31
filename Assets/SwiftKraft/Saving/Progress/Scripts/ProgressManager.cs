using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SwiftKraft.Saving.Progress
{
    /// <summary>
    /// A manager class for saving game progress.
    /// </summary>
    public static class ProgressManager
    {
        /// <summary>
        /// The array of folders leading up to the stored saves.
        /// </summary>
        public readonly static string[] SavesFilePath = { "Saves" };

        /// <summary>
        /// Nullable current progress save.
        /// </summary>
        public static ProgressSave Current { get; set; }

        /// <summary>
        /// Called when a new progress save is loaded.
        /// </summary>
        public static event Action OnCurrentChange;
        /// <summary>
        /// Called when the save files are refreshed.
        /// </summary>
        public static event Action OnFilesRefreshed;

        /// <summary>
        /// The list of save files that are detected.
        /// </summary>
        public static List<string> Files
        {
            get
            {
                if (_files == null)
                    RefreshFilesWithoutNotify();

                return _files;
            }

            private set => _files = value;
        }
        private static List<string> _files;

        /// <summary>
        /// Refreshes the save files without calling an event.
        /// </summary>
        public static void RefreshFilesWithoutNotify()
        {
            string path = Path.Combine(SaveManager.SavePath, Path.Combine(SavesFilePath));

            DirectoryInfo dir = new(path);
            FileInfo[] infos = dir.GetFiles("*.json");

            Debug.Log("Checking Files: Found " + infos.Length + " JSON Files.");

            _files ??= new();
            _files.Clear();
            foreach (FileInfo info in infos)
                _files.Add(info.Name.Replace(".json", ""));
        }

        /// <summary>
        /// Refreshes the save files.
        /// </summary>
        public static void RefreshFiles()
        {
            RefreshFilesWithoutNotify();

            OnFilesRefreshed?.Invoke();
        }

        /// <summary>
        /// Creates a new progress save and sets it to current.
        /// </summary>
        /// <param name="name">The name of the save file. (Excluding the .json extension)</param>
        public static void CreateProgress(string name)
        {
            Current = new() { Name = name };
            SaveProgress();
        }

        /// <summary>
        /// Deletes the progress save with the provided name. (Excluding the .json extension)
        /// </summary>
        /// <param name="name">The name of the save file. (Excluding the .json extension)</param>
        public static void DeleteProgress(string name)
        {
            if (Current.Name.Equals(name))
                Current = null;

            string path = Path.Combine(SaveManager.SavePath, Path.Combine(SavesFilePath), name + ".json");
            File.Delete(path);
            RefreshFiles();
        }

        /// <summary>
        /// Renames the current progress save.
        /// </summary>
        /// <param name="name">The new name of the save file. (Excluding the .json extension)</param>
        public static void RenameProgress(string name)
        {
            string path = Path.Combine(SaveManager.SavePath, Path.Combine(SavesFilePath), Current.Name + ".json");
            string pathNew = Path.Combine(SaveManager.SavePath, Path.Combine(SavesFilePath), name + ".json");

            File.Move(path, pathNew);
            Current.Name = name;

            SaveProgress(Current.Name);
            RefreshFiles();
        }

        /// <summary>
        /// Saves the current progress.
        /// </summary>
        public static void SaveProgress() => SaveProgress(Current.Name);

        /// <summary>
        /// Saves the current progress under a name.
        /// </summary>
        /// <param name="name">The name of the save file. (Excluding the .json extension)</param>
        public static void SaveProgress(string name)
        {
            Current.Name = name;
            SaveManager.Save(Current, name, SavesFilePath);
        }

        /// <summary>
        /// Loads a saved progress under the provided file name. (Excluding the .json extension)
        /// </summary>
        /// <param name="name">The provided save file name. (Excluding the .json extension)</param>
        /// <returns>Whether it is successful or not.</returns>
        public static bool LoadProgress(string name)
        {
            bool status = SaveManager.TryLoad(out ProgressSave save, name, SavesFilePath);

            if (status)
            {
                Current = save;
                Current.Name = name;
                OnCurrentChange?.Invoke();
            }

            return status;
        }
    }
}

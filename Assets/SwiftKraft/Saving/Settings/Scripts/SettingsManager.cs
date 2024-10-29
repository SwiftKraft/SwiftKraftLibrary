using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SwiftKraft.Saving.Settings
{
    /// <summary>
    /// A general manager for saving and loading setting profiles.
    /// </summary>
    public static class SettingsManager
    {
        /// <summary>
        /// The name for the default profile. (Excluding the .json extension)
        /// </summary>
        public const string DefaultProfileName = "Default";
        /// <summary>
        /// The name for the global file that remembers which profile the user is on. (Excluding the .json extension)
        /// </summary>
        public const string GlobalKey = "Settings";
        /// <summary>
        /// The array of folders leading up to the stored setting profiles.
        /// </summary>
        public readonly static string[] ProfileFilePath = { "Settings", "Profiles" };

        /// <summary>
        /// The global, remembers which setting profile the user is on.
        /// </summary>
        public static GlobalSetting Global
        {
            get
            {
                if (_global == null)
                {
                    if (!SaveManager.Globals.TryElement(GlobalKey, out _global))
                    {
                        _global = new();
                        Debug.LogError("Failed to get global settings!");
                    }
                    else
                        LoadProfile(_global.SelectedProfileName);
                }

                return _global;
            }
        }
        private static GlobalSetting _global;

        /// <summary>
        /// The current setting profile.
        /// </summary>
        public static SettingProfile Current
        {
            get
            {
                CheckDefault();
                return _current;
            }
        }
        private static SettingProfile _current;

        /// <summary>
        /// Checks if the default profile exists, it will create one if it doesn't.
        /// </summary>
        public static void CheckDefault()
        {
            if (_current == null && !LoadProfile(DefaultProfileName))
            {
                _current = new();
                SaveProfile(DefaultProfileName);
                Global.CheckFilesWithoutNotify();
            }
        }

        /// <summary>
        /// Event for changing setting profiles.
        /// </summary>
        public static event Action OnProfileChange;

        /// <summary>
        /// Creates a setting profile and sets it to the current one.
        /// </summary>
        /// <param name="name">The name of the file. (Excluding the .json extension)</param>
        /// <returns>The newly created setting profile.</returns>
        public static SettingProfile CreateProfile(string name)
        {
            SettingProfile prof = new();
            _current = prof;
            SaveProfile(name);
            Global.CheckFiles();
            return prof;
        }

        /// <summary>
        /// Deletes the current setting profile. (As well as its file on disk)
        /// </summary>
        public static void DeleteProfile()
        {
            string path = Path.Combine(SaveManager.SavePath, Path.Combine(ProfileFilePath), Current.Name + ".json");
            File.Delete(path);
            _current = null;
            Global.CheckFiles();
            CheckDefault();
        }

        /// <summary>
        /// Resets the current profile to its default values.
        /// </summary>
        public static void ResetProfile() => Current.Reset();

        /// <summary>
        /// Renames a setting profile.
        /// </summary>
        /// <param name="name">The new file name. (Excluding the .json extension)</param>
        public static void RenameProfile(string name)
        {
            string path = Path.Combine(SaveManager.SavePath, Path.Combine(ProfileFilePath), Current.Name + ".json");
            string pathNew = Path.Combine(SaveManager.SavePath, Path.Combine(ProfileFilePath), name + ".json");

            File.Move(path, pathNew);

            Current.Name = name;
            Global.SelectedProfileName = Current.Name;

            SaveProfile(Current.Name);
            Global.CheckFiles();
        }

        /// <summary>
        /// Saves the current profile.
        /// </summary>
        public static void SaveProfile() => SaveProfile(Global.SelectedProfileName);

        /// <summary>
        /// Saves the current profile under a provided name.
        /// </summary>
        /// <param name="name">The name of the file. (Excluding the .json extension)</param>
        public static void SaveProfile(string name)
        {
            _current.Name = name;
            SaveManager.Save(_current, name, ProfileFilePath);
        }

        /// <summary>
        /// Loads a setting profile file and sets it to the current one.
        /// </summary>
        /// <param name="name">The name of the file. (Excluding the .json extension)</param>
        /// <returns>Whether or not the load is successful.</returns>
        public static bool LoadProfile(string name)
        {
            bool status = SaveManager.TryLoad(out SettingProfile prof, name, ProfileFilePath);

            if (status)
            {
                _current = prof;
                _current.Name = name;
                Global.SelectedProfileName = Current.Name;
                SaveManager.SaveGlobal();
                OnProfileChange?.Invoke();
            }

            return status;
        }

        /// <summary>
        /// The object for the global.
        /// </summary>
        public class GlobalSetting : GlobalElement
        {
            /// <summary>
            /// The name of the currently selected setting profile. (Excluding the .json extension)
            /// </summary>
            [JsonProperty]
            public string SelectedProfileName = DefaultProfileName;
            /// <summary>
            /// The list of the names of the detected profiles. (Excluding the .json extension)
            /// </summary>
            public List<string> Profiles
            {
                get
                {
                    if (_profiles == null)
                        CheckFilesWithoutNotify();

                    return _profiles;
                }
            }

            /// <summary>
            /// The event for when the list of detected profiles is updated.
            /// </summary>
            public event Action OnProfilesUpdated;

            private List<string> _profiles;

            /// <summary>
            /// Refreshes the list of detected profiles without sending an event.
            /// </summary>
            public void CheckFilesWithoutNotify()
            {
                string path = Path.Combine(SaveManager.SavePath, Path.Combine(ProfileFilePath));

                DirectoryInfo dir = new(path);
                FileInfo[] infos = dir.GetFiles("*.json");

                Debug.Log("Checking Files: Found " + infos.Length + " JSON Files.");

                _profiles ??= new();
                _profiles.Clear();
                foreach (FileInfo info in infos)
                    _profiles.Add(info.Name.Replace(".json", ""));
            }

            /// <summary>
            /// Refreshes the list of detected profiles.
            /// </summary>
            public void CheckFiles()
            {
                CheckFilesWithoutNotify();

                OnProfilesUpdated?.Invoke();
            }
        }
    }
}

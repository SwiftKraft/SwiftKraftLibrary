// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;

    internal static class ProjectSimulatorSlugStore
    {
        private static Dictionary<string, string> projectSlugs = new();

        static ProjectSimulatorSlugStore() => LoadValues();

        public static void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            projectSlugs[key] = value;
            Save();
        }

        public static string Get(string key) => projectSlugs.GetValueOrDefault(key);

        private static void Save()
        {
            var json = JsonConvert.SerializeObject(projectSlugs);
            if (File.Exists(Paths.simulatorProjectSlugsPath))
            {
                File.Delete(Paths.simulatorProjectSlugsPath);
            }

            File.WriteAllText(Paths.simulatorProjectSlugsPath, json);
        }

        /// <summary>
        /// Keep only the keys that match the filter.
        /// </summary>
        /// <param name="filter">Filter containing the keys to keep.</param>
        public static void KeepOnly(Predicate<string> filter)
        {
            projectSlugs = projectSlugs.Where(pair => filter(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            Save();
        }

        private static void LoadValues()
        {
            var directory = Path.GetDirectoryName(Paths.simulatorProjectSlugsPath);
            Directory.CreateDirectory(directory);

            if (!File.Exists(Paths.simulatorProjectSlugsPath))
            {
                return;
            }

            var json = File.ReadAllText(Paths.simulatorProjectSlugsPath);
            try
            {
                projectSlugs = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ??
                               new Dictionary<string, string>();
            }
            catch (JsonSerializationException)
            {
                projectSlugs = new Dictionary<string, string>();
            }
        }
    }
}

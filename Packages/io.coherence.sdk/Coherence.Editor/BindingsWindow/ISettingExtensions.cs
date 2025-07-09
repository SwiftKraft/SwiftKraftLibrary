// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="ISetting"/>-related extension methods.
    /// </summary>
    internal static class ISettingExtensions
    {
        public static void SetAutoSave(this IEnumerable<ISetting> settings, bool autoSave)
        {
            foreach (var setting in settings)
            {
                setting.AutoSave = autoSave;
            }
        }

        public static void Save(this IEnumerable<ISetting> settings)
        {
            foreach (var setting in settings)
            {
                setting.Save();
            }
        }

        public static void Discard(this IEnumerable<ISetting> settings)
        {
            foreach (var setting in settings)
            {
                setting.Discard();
            }
        }

        public static bool HaveUnappliedChanges(this IEnumerable<ISetting> settings)
        {
            foreach (var setting in settings)
            {
                if (setting.HasUnsavedChanges)
                {
                    return true;
                }
            }

            return false;
        }

        public static void AddChangedListener<TValue>(this IEnumerable<ISetting<TValue>> settings, Action<TValue> listener)
        {
            foreach (var setting in settings)
            {
                setting.Changed += listener;
            }
        }

        public static void RemoveChangedListener<TValue>(this IEnumerable<ISetting<TValue>> settings, Action<TValue> listener)
        {
            foreach (var setting in settings)
            {
                setting.Changed -= listener;
            }
        }
    }
}

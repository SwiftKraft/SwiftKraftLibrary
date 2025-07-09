// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor
{
    using System;

    /// <summary>
    /// Represents a setting of type <see cref="TValue"/> that can be persisted automatically or manually.
    /// </summary>
    internal interface ISetting<TValue> : ISetting, IEquatable<TValue>
    {
        /// <summary>
        /// Event that is raised whenever the <see cref="Value"/> of this setting changes.
        /// </summary>
        event Action<TValue> Changed;

        /// <summary>
        /// The current value of this setting.
        /// </summary>
        TValue Value { get; set; }
    }

    /// <summary>
    /// Represents a setting that can be persisted automatically or manually.
    /// </summary>
    internal interface ISetting
    {
        /// <summary>
        /// Does the local value for this setting differ from the one in persistent storage?
        /// </summary>
        bool HasUnsavedChanges { get; }

        /// <summary>
        /// When <see cref="AutoSave"/> is enabled, the boolean value is read from and written directly to the storage;
        /// otherwise, the value is cached locally and only written to the storage when <see cref="Save"/> is called.
        /// </summary>
        bool AutoSave { get; set; }

        /// <summary>
        /// Saves any unapplied changes to persistent storage.
        /// </summary>
        void Save();

        /// <summary>
        /// Discards any unapplied changes and replaces locally cached value with one from persistent storage.
        /// </summary>
        void Discard();
    }
}

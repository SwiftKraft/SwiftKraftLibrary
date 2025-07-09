// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Common;
    using System.Collections.Generic;
    using UnityEngine;

    internal interface IDataMigrator
    {
        /// <summary>
        ///     Maximum version that this migrator will support, if the current SDK version is equal or above the Max
        ///     Supported Version, the migrator will not execute and an error will be logged, reminding you that you should
        ///     consider removing the migrator and its related obsolete code, or reevaluating the Max Supported Version for this
        ///     migrator.
        /// </summary>
        SemVersion MaxSupportedVersion { get; }

        /// <summary>
        ///     Message that will be shown in the Welcome Window if the migrator is successful.
        /// </summary>
        string MigrationMessage { get; }

        /// <summary>
        ///     Used to order migrators execution.  Migrators with lower values are called before ones with higher values.
        /// </summary>
        int Order { get; }

        /// <summary>
        ///     Method that will be run once on package update, before attempting to migrate the Objects returned by GetMigrationTargets.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Load the Unity Objects that this migrator will attempt to postprocess.
        /// </summary>
        IEnumerable<Object> GetMigrationTargets();

        /// <summary>
        ///     Method that will be called on package update or when postprocessing a coherence asset after importing it.
        /// </summary>
        bool RequiresMigration(Object obj);

        /// <summary>
        ///     Method that will be called on package update or when postprocessing a coherence asset after importing it.
        /// </summary>
        bool MigrateObject(Object obj);
    }
}

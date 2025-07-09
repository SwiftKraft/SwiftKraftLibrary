// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Transport;
    using Common;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [Preserve]
    internal class RuntimeSettingsTransportMigrator : IDataMigrator
    {
        public SemVersion MaxSupportedVersion => new SemVersion(2);
        public int Order => 2;
        public string MigrationMessage => "Updated RuntimeSettings with new transport type settings.";

        public void Initialize()
        {
        }

        public IEnumerable<Object> GetMigrationTargets()
        {
            yield return RuntimeSettings.Instance;
        }

        public bool RequiresMigration(Object obj)
        {
            if (obj is not RuntimeSettings settings)
            {
                return false;
            }

            return !settings.defaultTransportModeMigrated;
        }

        public bool MigrateObject(Object obj)
        {
            if (obj is not RuntimeSettings settings)
            {
                return false;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            switch (settings.defaultTransportMode)
            {
                case DefaultTransportMode.UDPWithTCPFallback:
                    settings.TransportType = TransportType.UDPWithTCPFallback;
                    break;
                case DefaultTransportMode.UDPOnly:
                    settings.TransportType = TransportType.UDPOnly;
                    break;
                case DefaultTransportMode.TCPOnly:
                    settings.TransportType = TransportType.TCPOnly;
                    break;
                case DefaultTransportMode.UDPExperimental:
                    settings.TransportType = TransportType.UDPOnly;
                    settings.TransportConfiguration = TransportConfiguration.ManagedWithExperimentalUDP;
                    break;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            settings.defaultTransportModeMigrated = true;

            EditorUtility.SetDirty(obj);

            return true;
        }
    }
}

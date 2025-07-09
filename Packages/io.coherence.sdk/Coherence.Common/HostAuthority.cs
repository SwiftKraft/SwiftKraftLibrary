// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;
    using System.Linq;

    /// <summary>
    /// Advanced simulator authority feature flags for the Replication Server.
    /// </summary>
    [Flags]
    public enum HostAuthority
    {
        /// <summary>
        /// The Replication Server will restrict entity creation to only the host or simulator.
        /// </summary>
        CreateEntities = 1 << 0,

        /// <summary>
        /// The Replication Server will validate new connection with the host or simulator.
        /// </summary>
        ValidateConnection = 1 << 1,
    }

    /// <summary>
    /// <see cref="HostAuthority"/> extension methods.
    /// </summary>
    public static class HostAuthorityEx
    {
        /// <summary>
        /// Checks if any feature is enabled.
        /// </summary>
        /// <param name="authority">Feature flags.</param>
        /// <returns>True if any feature flag is enabled.</returns>
        public static bool HasFeatures(this HostAuthority authority)
        {
            return authority != 0;
        }

        /// <summary>
        /// Checks if a specific feature is enabled.
        /// </summary>
        /// <param name="authority">Feature flags.</param>
        /// <param name="feature">The feature flag you want to check for.</param>
        /// <returns>True if the <paramref name="authority"/> flags has <paramref name="feature"/> flag enabled.</returns>
        public static bool Can(this HostAuthority authority, HostAuthority feature)
        {
            return (authority & feature) != 0;
        }

        /// <summary>
        /// Formats the flags into a comma-separated string.
        /// </summary>
        /// <param name="authority">Feature flags for formatting.</param>
        /// <returns>
        /// A string of comma-separated dash-cased enabled feature flags.
        /// Or empty string if no feature flags are enabled.
        /// </returns>
        public static string GetCommaSeparatedFeatures(this HostAuthority authority)
        {
            var stringBuilder = new System.Text.StringBuilder();

            foreach (HostAuthority feature in Enum.GetValues(typeof(HostAuthority)))
            {
                if (authority.Can(feature))
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append(",");
                    }

                    stringBuilder.Append(CamelCaseToDashCase(feature.ToString()));
                }
            }

            static string CamelCaseToDashCase(string text)
            {
                return string.Concat(text.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x : x.ToString())).ToLower();
            }

            return stringBuilder.ToString();
        }
    }
}

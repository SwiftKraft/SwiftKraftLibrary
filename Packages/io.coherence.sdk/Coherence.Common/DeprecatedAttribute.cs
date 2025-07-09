// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System;

    internal class DeprecatedAttribute : Attribute
    {
        public int VersionMajor { get; }
        public int VersionMinor { get; }
        public int VersionPatch { get; }

        public string AsOf { get; }

        public string Reason { get; set; }

        /// <param name="asOf">Date of deprecation. DD/MM/YYYY (MM/YYYY preferred for simplicity and unambiguity).</param>
        /// <param name="versionMajor">MAJOR version number at which this deprecation was introduced.</param>
        /// <param name="versionMinor">MINOR version number at which this deprecation was introduced.</param>
        /// <param name="versionPatch">PATCH version number at which this deprecation was introduced.</param>
        public DeprecatedAttribute(string asOf, int versionMajor, int versionMinor, int versionPatch)
        {
            AsOf = asOf;
            VersionMajor = versionMajor;
            VersionMinor = versionMinor;
            VersionPatch = versionPatch;
        }
    }
}

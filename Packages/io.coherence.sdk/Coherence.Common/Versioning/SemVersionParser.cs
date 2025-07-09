// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using System;

namespace Coherence.Common
{
    internal static class SemVersionParser
    {
        public static SemVersion Parse(string version)
        {
            int cursor = 0;

            int major = 0;
            int minor = 0;
            int patch = 0;
            string prerelease = null;
            string build = null;

            //Doing this instead because RegEx is impressively slow
            try
            {
                major = int.Parse(
                    VersionUtils.ConsumeVersionComponentFromString(version, ref cursor, x => !char.IsDigit(x)));
                if (cursor < version.Length && version[cursor] == '.')
                {
                    cursor++;
                    minor = int.Parse(
                        VersionUtils.ConsumeVersionComponentFromString(version, ref cursor, x => !char.IsDigit(x)) ??
                        "0");
                }

                if (cursor < version.Length && version[cursor] == '.')
                {
                    cursor++;
                    patch = int.Parse(
                        VersionUtils.ConsumeVersionComponentFromString(version, ref cursor, x => !char.IsDigit(x)) ??
                        "0");
                }

                if (cursor < version.Length && version[cursor] == '-')
                {
                    cursor++;
                    prerelease = VersionUtils.ConsumeVersionComponentFromString(version, ref cursor, x => x == '+');
                }

                if (cursor < version.Length && version[cursor] == '+')
                {
                    cursor++;
                    build = VersionUtils.ConsumeVersionComponentFromString(version, ref cursor, x => x == '\0');
                }
            }
            catch (Exception)
            {
                throw new ArgumentException($"{version} is not valid Semantic Version");
            }


            return new SemVersion(major, minor, patch, prerelease, build);
        }

        public static bool TryParse(string versionString, out SemVersion? result)
        {
            try
            {
                result = SemVersionParser.Parse(versionString);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }
    }
}

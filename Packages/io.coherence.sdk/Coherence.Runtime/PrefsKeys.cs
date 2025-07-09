// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    internal static class PrefsKeys
    {
        public const string AnalyticsId = "Coherence.Proj{0}.Id{1}.AnalyticsId";

        // Project specific
        public const string UniqueIdPool = "Coherence.Proj{0}.UniqueIdPool";

        /// <summary>
        /// Prefs key used to store the id of a guest that has logged in to a particular project using a particular Unique Id.
        /// </summary>
        public const string CachedGuestId = "Coherence.Proj{0}.Id{1}.GuestId";

        /// <summary>
        /// Prefs key used to store the username of the last player account that was logged in, in coherence 1.3.0 and older.
        /// </summary>
        public const string CachedLegacyUsername = "Coherence.Proj{0}.Id{1}.AuthUserName";

        /// <summary>
        /// Prefs key used to store the password of the last player account that was logged in as a guest, in coherence 1.3.0 and older.
        /// </summary>
        public const string CachedLegacyGuestPassword = "Coherence.Proj{0}.Id{1}.AuthGuestPassword";

        /// <summary>
        /// Prefs key used to store the session token of the last player account that was logged in, in coherence 1.3.0 and older.
        /// </summary>
        public const string CachedLegacySessionToken = "Coherence.Proj{0}.Id{1}.AuthSessionToken";
    }

    namespace Utils
    {
        internal static class PrefsUtils
        {
            public static string Format(this string text, object arg1)
            {
                return string.Format(text, arg1);
            }

            public static string Format(this string text, params object[] args)
            {
                return string.Format(text, args);
            }
        }
    }
}

// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Editor
{
    /// <summary>
    /// Lists various web addresses of interest.
    /// </summary>
    internal static class Urls
    {
        /// <summary>
        /// coherence discord server addresses.
        /// </summary>
        public static class Discord
        {
            /// <summary>
            /// #help channel.
            /// </summary>
            public const string HelpChannel = Main + "/1085161745263370310";

            private const string Main = "https://discord.com/channels/618322516762558466";
        }

        /// <summary>
        /// coherence community site.
        /// </summary>
        public static class Community
        {
            /// <summary>
            /// Bug reports page.
            /// <para>
            /// Find Help, submit your Bug Reports and your Suggestions here.
            /// </para>
            /// </summary>
            public const string BugReports = Category + "/bug-reports";

            private const string Main = "https://community.coherence.io";
            private const string Category = Main + "/c";
        }
    }
}

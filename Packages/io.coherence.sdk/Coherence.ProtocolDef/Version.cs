// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.ProtocolDef
{
    // Version used by serializers and other protocol logic to decide actions to take
    // based on the value of these constants.
    public static class Version
    {
        // Version info for protocol changes per version update.
        // When a new protocol breaking change is added we do two things:
        //
        // 1. Update the CurrentVersion by incrementing by 1.
        // 2. Add a new constant with a value equal to the new current version
        //    where the variable name is descriptive of the change with a comment about the change.
        //
        //    For example, if the initial state of this file is:
        //
        //    const uint CurrentVersion = 1;
        //
        //    And we're adding a new floating origin optimization that is protocol
        //    breaking, then the new state of this file would be:
        //
        //    const uint uint CurrentVersion = 2;
        //    const uint uint FloatingOriginOptimization = 2; // Added new field to the FO component.
        //
        //    Only the CurrentVersion should change and only new constants are added, we should
        //    never change the value of the added constants.

        public const uint CurrentVersion = 7;

        // Using VersionIncludes for added protocol change.
        // Would use VersionExcludes if the protocol change removed something.

        public const uint VersionIncludesConnectResponseHostPayload = 7;
        public const uint VersionIncludesConnectionKicking = 6;
        public const uint VersionIncludesConnectionValidation = 5;
        public const uint VersionIncludesChannelID = 4;
        public const uint VersionIncludesEndOfMessagesMarker = 3;
        public const uint VersionIncludesConnectInfoMTU = 2;
    }
}

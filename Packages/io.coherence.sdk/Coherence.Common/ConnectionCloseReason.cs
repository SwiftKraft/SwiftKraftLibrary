// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    // Make sure this matches the brisk definition in the engine.
    // brisk/commands/disconnect_request.go
    public enum ConnectionCloseReason : byte
    {
        Unknown = 0,
        InvalidChallenge = 1,
        ServerError = 2,
        MaxEntitiesReached = 3,
        RoomFull = 4,
        GracefulClose = 5,
        InvalidData = 6,
        Timeout = 7,
        RoomNotFound = 8,
        ReceiveFrequencyExceeded = 9,
        PersistenceNotReady = 10,
        VersionIncompatible = 11,
        ServerHighLoad = 12,
        // Never intentionally sent, just a result of TCP closing
        // before message arrives.
        SocketClosedByPeer = 13,
        HostNotReady = 14, // used by hostauthority.ValidateConnection
        ConnectionRejectedByHost = 15, // used by hostauthority.ValidateConnection
        ConnectionValidationTimeout = 16, // used by hostauthority.ValidateConnection
        KickedByHost = 17,
        HostDisconnected = 18, // used by hostauthority features
    }
}

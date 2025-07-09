// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System.Collections.Generic;

    public class FindLobbyOptions
    {
        public List<LobbyFilter> LobbyFilters;
        public Dictionary<SortOptions, bool> Sort;
        public int Limit = 10;

        public static FindLobbyOptions Default => new FindLobbyOptions();
    }

    public enum SortOptions
    {
        createdAt,
        tag,
        maxPlayers,
        numPlayers
    }
}

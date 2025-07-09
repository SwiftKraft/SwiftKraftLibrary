// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System.Collections.Generic;

    public class CreateLobbyOptions
    {
        public string Region;
        public string Name;
        public string Tag;
        public int MaxPlayers;
        public bool Unlisted;
        public string Secret;
        public string SimulatorSlug;
        public List<CloudAttribute> LobbyAttributes;
        public List<CloudAttribute> PlayerAttributes;

        public static CreateLobbyOptions Default => new CreateLobbyOptions();
    }
}

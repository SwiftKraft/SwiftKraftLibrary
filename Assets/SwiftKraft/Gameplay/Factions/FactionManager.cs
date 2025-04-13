using SwiftKraft.Gameplay.Interfaces;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwiftKraft.Gameplay.Factions
{
    public static class FactionManager
    {
        private static readonly Dictionary<string, Faction> Factions = new();

        public static bool HasFaction(string id) => Factions.ContainsKey(id);

        public static Faction GetFaction(string id, string name = "Unnamed Faction")
        {
            if (Factions.ContainsKey(id))
                return Factions[id];
            Faction f = new(id, name);
            Factions.Add(id, f);
            return f;
        }

        public static void RemoveFaction(string id)
        {
            if (Factions.ContainsKey(id))
                Factions[id].Dispose();
            Factions.Remove(id);
        }

        public static void ClearFactions()
        {
            foreach (Faction f in Factions.Values)
                f.Dispose();
            Factions.Clear();
        }
    }

    public class Faction : IFaction, IDisposable
    {
        public readonly string Name;
        public readonly string ID;

        public readonly Dictionary<string, object> Statistics = new();

        public bool FriendlyFire { get; set; }
        public bool Disposed { get; private set; } = false;

        public Faction(string id, string name) : base()
        {
            ID = id;
            Name = name;
        }

        string IFaction.Faction { get => ID; set { } }

        public bool TryGetStat<T>(string id, out T stat)
        {
            if (Statistics.ContainsKey(id) && Statistics[id] is T t)
            {
                stat = t;
                return true;
            }
            else
            {
                stat = default;
                return false;
            }
        }

        public void Dispose() => Disposed = true;

        public static implicit operator string(Faction f) => f.ID;
        public static implicit operator Faction(string id) => FactionManager.GetFaction(id);
    }
}

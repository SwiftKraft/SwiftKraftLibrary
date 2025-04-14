using SwiftKraft.Gameplay.Factions;
using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Bases
{
    public abstract class FactionBehaviourBase : MonoBehaviour, IFaction
    {
        public FactionCore FactionCore { get; private set; }

        public virtual string Faction
        {
            get
            {
                if (FactionCore == null)
                    RefreshFactionCore();
                return FactionCore == null ? "" : FactionCore.Faction;
            }
            set
            {
                if (FactionCore == null)
                    RefreshFactionCore();

                if (FactionCore == null)
                    FactionCore.Faction = value;
            }
        }

        public Faction FactionObject => FactionCore != null ? FactionCore.FactionObject : null;

        public void RefreshFactionCore()
        {
            if (TryGetComponent(out FactionCore core) || this.TryGetComponentInParent(out core))
                FactionCore = core;
        }
    }
}

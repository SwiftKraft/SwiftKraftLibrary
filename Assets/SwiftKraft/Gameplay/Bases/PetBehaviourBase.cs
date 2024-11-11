using SwiftKraft.Gameplay.Interfaces;

namespace SwiftKraft.Gameplay.Bases
{
    public abstract class PetBehaviourBase : EntityBehaviourBase, IPet
    {
        public override string Faction
        {
            get => Owner != null ? Owner.Faction : default;
            set
            {
                if (Owner != null)
                    Owner.Faction = value;
            }
        }

        public IEntity Owner
        {
            get => (object)_owner == this ? null : _owner;
            set
            {
                if ((object)value == this)
                    return;
                _owner = value;
                OnOwnerChanged();
            }
        }
        IEntity _owner;

        protected virtual void OnOwnerChanged() { }
    }
}

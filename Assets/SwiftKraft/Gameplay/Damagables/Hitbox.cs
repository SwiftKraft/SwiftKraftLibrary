using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Damagables
{
    public class Hitbox : MonoBehaviour, IDamagable, IFaction
    {
        public IDamagable Parent { get; private set; }
        public string Faction
        {
            get => Parent is IFaction f ? f.Faction : default;
            set
            {
                if (Parent is IFaction f)
                    f.Faction = value;
            }
        }

        protected virtual void Awake()
        {
            Parent = transform.parent.GetComponentInParent<IDamagable>();

            if ((Object)Parent == this)
                Parent = null;
        }

        public void Damage(DamageDataBase data) => Parent?.Damage(data);
    }
}

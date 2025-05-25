using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Damagables
{
    public class Hurtbox : MonoBehaviour, IDamagable, IFaction, IKillable
    {
        public IDamagable Parent { get; private set; }
        public virtual string Faction
        {
            get => Parent is IFaction f ? f.Faction : default;
            set
            {
                if (Parent is IFaction f)
                    f.Faction = value;
            }
        }

        public bool IsDead => Parent is IKillable killable && killable.IsDead;

        protected virtual void Awake()
        {
            Parent = transform.parent.GetComponentInParent<IDamagable>();

            if ((Object)Parent == this)
                Parent = null;
        }

        public virtual void Damage(DamageDataBase data)
        {
            if ((Object)Parent != null)
                data.ApplyDamage(Parent);
        }
    }
}

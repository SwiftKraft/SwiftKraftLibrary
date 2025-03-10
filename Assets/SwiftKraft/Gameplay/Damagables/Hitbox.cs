using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Damagables
{
    public class Hitbox : MonoBehaviour, IDamagable
    {
        public IDamagable Parent { get; private set; }

        protected virtual void Awake()
        {
            Parent = transform.root.GetComponent<IDamagable>();

            if ((Object)Parent == this)
                Parent = null;
        }

        public void Damage(DamageDataBase data) => Parent?.Damage(data);
    }
}

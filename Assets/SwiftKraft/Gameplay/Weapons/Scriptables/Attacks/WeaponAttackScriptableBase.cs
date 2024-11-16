using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Weapons/Attacks/Base", fileName = "New Attack", order = 0)]
    public class WeaponAttackScriptableBase : ScriptableObject
    {
        [HideInInspector]
        public WeaponBase Parent;

        public GameObject Prefab;

        public virtual void Attack(Transform origin)
        {
            GameObject go = Instantiate(Prefab, origin.position, origin.rotation);
            if (go.TryGetComponent(out IPet pet))
                pet.Owner = Parent.GetRootOwner();
        }

        public virtual void Tick() { }
    }
}

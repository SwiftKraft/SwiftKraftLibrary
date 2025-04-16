using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    [CreateAssetMenu(menuName = "SwiftKraft/Gameplay/Weapons/Weapon", fileName = "New Weapon", order = 0)]
    public class WeaponScriptable : ScriptableObject
    {
        public WeaponAttackBase[] Attacks;
    }
}

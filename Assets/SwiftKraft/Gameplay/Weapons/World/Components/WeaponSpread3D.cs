using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponSpread3D : WeaponSpread
    {
        public override void ApplySpread() => Rotation = Quaternion.Euler(Random.insideUnitCircle * GetSpread());
    }
}

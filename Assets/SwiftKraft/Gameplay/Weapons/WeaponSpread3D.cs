using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponSpread3D : WeaponSpread
    {
        public override void Randomize(Transform target) => target.localRotation *= Quaternion.Euler(Random.insideUnitCircle * Current);
    }
}

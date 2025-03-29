using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class AttachmentAimSpreadProperty : AttachmentStatisticPropertyBase<WeaponSpread>
    {
        public override ModifiableStatistic.Modifier CreateOverrider() => Component.AimMultiplier.AddModifier();
    }
}

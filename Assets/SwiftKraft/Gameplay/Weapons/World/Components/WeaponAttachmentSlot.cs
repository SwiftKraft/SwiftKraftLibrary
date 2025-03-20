using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAttachmentSlot : MonoBehaviour
    {
        public WeaponAttachments Parent { get; private set; }

        public int AttachmentIndex { get; set; }

        public void Initialize(WeaponAttachments parent) => Parent = parent;
    }
}
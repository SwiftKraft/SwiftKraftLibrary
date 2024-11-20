using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponEquipperCommunicator : AnimatorCommunicator<WeaponEquipper>
    {
        public string StateID = "Equip";

        protected virtual void Awake()
        {
            ParentComponent.OnStartEquip += OnStartEquip;
            ParentComponent.OnStartEquip += OnEndEquip;
        }

        protected virtual void OnDestroy()
        {
            ParentComponent.OnStartEquip -= OnStartEquip;
            ParentComponent.OnStartEquip -= OnEndEquip;
        }

        protected virtual void OnStartEquip()
        {
            
        }

        protected virtual void OnEndEquip()
        {

        }
    }
}

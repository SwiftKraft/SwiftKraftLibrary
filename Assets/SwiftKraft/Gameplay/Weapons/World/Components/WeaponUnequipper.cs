using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponUnequipper : WeaponComponentBlocker
    {
        public const string UnequipAction = "Equip";

        public bool AlwaysUnequip;

        protected override void Awake()
        {
            base.Awake();
            Parent.AddAction(UnequipAction, StartUnequip);
        }

        protected virtual void OnDisable()
        {
            if (AlwaysUnequip)
            {
                gameObject.SetActive(true);
                Parent.StartAction(UnequipAction);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Parent.Actions.Remove(UnequipAction);
        }

        public virtual bool StartUnequip()
        {
            AttackDisabler.Active = true;
            return true;
        }

        public virtual void EndUnequip()
        {
            AttackDisabler.Active = false;
            gameObject.SetActive(false);
        }
    }
}
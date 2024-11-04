using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponBase : MonoBehaviour
    {
        [field: SerializeField]
        public Transform AttackOrigin { get; private set; }
        public List<AttackMode> AttackModes
        {
            get
            {
                if (_attackModes == null)
                {
                    _attackModes = new();
                    RefreshAttackModes();
                }

                return _attackModes;
            }
        }
        List<AttackMode> _attackModes;

        public int CurrentModeIndex
        {
            get => _currentMode;
            private set
            {
                value %= AttackModes.Count;
                OnAttackModeUpdated?.Invoke(value);
                _currentMode = value;
            }
        }
        int _currentMode;

        public readonly BooleanLock CanAttack = new();

        public AttackMode CurrentMode => AttackModes.InRange(CurrentModeIndex) ? AttackModes[CurrentModeIndex] : null;

        public event Action<int> OnAttackModeUpdated;

        public void RefreshAttackModes() => RefreshAttackModes(_attackModes);

        public abstract void RefreshAttackModes(List<AttackMode> modesList);

        public virtual void Attack() => Attack(AttackOrigin);

        public virtual void Attack(Transform origin)
        {
            if (!CanAttack || origin == null)
                return;

            CurrentMode?.Attack(origin);
        }

        public abstract class AttackMode
        {
            public abstract void Attack(Transform origin);
        }

        public abstract class AttackMode<T> : AttackMode where T : WeaponBase
        {
            public readonly T Parent;

            public AttackMode(T parent) => Parent = parent;
        }
    }
}

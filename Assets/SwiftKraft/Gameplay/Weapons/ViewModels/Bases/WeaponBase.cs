using SwiftKraft.Gameplay.Bases;
using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponBase : PetBehaviourBase
    {
        [field: SerializeField]
        public WeaponScriptable Scriptable { get; private set; }

        [field: SerializeField]
        public Transform AttackOrigin { get; private set; }

        public List<WeaponAttackScriptableBase> AttackModeCache
        {
            get
            {
                if (_attackModeCache == null)
                {
                    _attackModeCache = new();
                    RefreshAttackModes();
                }

                return _attackModeCache;
            }
        }
        List<WeaponAttackScriptableBase> _attackModeCache;

        public int CurrentModeIndex
        {
            get => _currentMode;
            private set
            {
                value %= AttackModeCache.Count;
                OnAttackModeUpdated?.Invoke(value);
                _currentMode = value;
            }
        }
        int _currentMode;
        public readonly BooleanLock CanAttack = new();

        public WeaponAttackScriptableBase CurrentMode => AttackModeCache.InRange(CurrentModeIndex) ? AttackModeCache[CurrentModeIndex] : null;

        public event Action<int> OnAttackModeUpdated;

        protected virtual void Awake() => Owner = transform.root.GetComponentInChildren<IEntity>();

        public void RefreshAttackModes()
        {
            if (Scriptable == null)
            {
                Debug.LogError("Weapon view model \"" + gameObject.name + "\" does not have an assigned WeaponScriptable!");
                return;
            }

            DestroyAttackModes();
            CloneAttackModes();
        }

        public void DestroyAttackModes()
        {
            if (_attackModeCache.Count > 0)
                foreach (WeaponAttackScriptableBase atk in _attackModeCache)
                    DestroyImmediate(atk, false);
            _attackModeCache.Clear();
        }

        public void CloneAttackModes()
        {
            foreach (WeaponAttackScriptableBase atk in Scriptable.Attacks)
            {
                WeaponAttackScriptableBase scr = Instantiate(atk);
                scr.Parent = this;
                AttackModeCache.Add(scr);
            }
        }

        public virtual void Attack() => Attack(AttackOrigin);

        public virtual void Attack(Transform origin)
        {
            if (!CanAttack || origin == null)
                return;

            if (CurrentMode != null)
                CurrentMode.Attack(origin);
        }

        protected virtual void FixedUpdate()
        {
            if (CurrentMode != null)
                CurrentMode.Tick();
        }

        protected virtual void OnDestroy() => DestroyAttackModes();
    }
}

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
        public const string AttackAction = "Attack";

        public readonly Dictionary<string, Func<bool>> Actions = new();

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
        public event Action OnAttack;

        protected virtual void Awake()
        {
            Owner = transform.root.GetComponentInChildren<IPawn>();
            Actions.Add(AttackAction, Attack);
        }

        protected virtual void OnDestroy()
        {
            DestroyAttackModes();
            Actions.Remove(AttackAction);
        }

        public bool StartAction(string id) => Actions.ContainsKey(id) && Actions[id].Invoke();

        public void AttackEvent() => OnAttack?.Invoke();

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

        public virtual bool Attack() => Attack(AttackOrigin);

        public virtual bool Attack(Transform origin)
        {
            if (!CanAttack || origin == null)
                return false;

            if (CurrentMode != null)
            {
                CurrentMode.Attack(origin);
                return true;
            }

            return false;
        }

        protected virtual void FixedUpdate()
        {
            if (CurrentMode != null)
                CurrentMode.Tick();
        }
    }
}

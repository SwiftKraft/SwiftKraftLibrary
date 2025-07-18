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
        public static event Action<WeaponBase> OnWeaponAttack;
        public static event Action<WeaponBase, GameObject> OnWeaponSpawn;

        public class WeaponAction
        {
            public readonly Func<bool> Function;
            public bool Status
            {
                get => _status;
                set
                {
                    if (_status == value)
                        return;

                    OnChanged?.Invoke(value);
                    _status = value;
                }
            }
            bool _status;

            public event Action<bool> OnChanged;

            public WeaponAction(Func<bool> func) => Function = func;
        }

        public const string AttackAction = "Attack";
        public const string SwitchAction = "SwitchMode";

        public readonly Dictionary<string, WeaponAction> Actions = new();

        [field: SerializeField]
        public Transform AttackOrigin { get; private set; }

        public ModifiableStatistic Damage;

        public virtual bool Attacking => CurrentMode.Attacking;

        public int CurrentModeIndex
        {
            get => _currentMode;
            private set
            {
                if (AttackModes.Count <= 0)
                    return;

                value %= AttackModes.Count;
                CurrentMode?.End();
                OnAttackModeUpdated?.Invoke(value);
                _currentMode = value;
                CurrentMode?.Begin();
            }
        }
        int _currentMode;
        public readonly BooleanLock CanAttack = new();

        public WeaponAttackBase CurrentMode => AttackModes.InRange(CurrentModeIndex) ? AttackModes[CurrentModeIndex] : null;

        [field: SerializeReference, Subclass(IsList = true)]
        public List<WeaponAttackBase> AttackModes { get; private set; } = new();

        public event Action<int> OnAttackModeUpdated;
        public event Action<string> OnStartAction;
        public event Action<string> OnAttemptAction;
        public event Action<string> OnEvent;
        public event Action<GameObject[]> OnAttack;
        public event Action OnPreAttack;
        public event Action<GameObject> OnSpawn;
        public event Action OnPreSpawn;

        protected virtual void Awake()
        {
            Owner = transform.root.GetComponentInChildren<IPawn>();
            AddAction(AttackAction, Attack);
            AddAction(SwitchAction, SwitchMode);

            foreach (WeaponAttackBase attack in AttackModes)
                attack.Parent = this;
        }

        protected virtual void OnDestroy() => Actions.Remove(AttackAction);

        protected virtual void Start() => CurrentMode?.Begin();

        public WeaponAction AddAction(string id, Func<bool> func)
        {
            if (Actions.ContainsKey(id))
                return null;
            WeaponAction action = new(func);
            Actions.Add(id, action);
            return action;
        }

        public bool TryAddAction(string id, Func<bool> func, out WeaponAction action)
        {
            action = AddAction(id, func);
            return action != null;
        }

        public void UpdateAction(string id, bool status)
        {
            if (Actions.ContainsKey(id))
                Actions[id].Status = status;
        }

        public bool StartAction(string id)
        {
            bool status = Actions.ContainsKey(id) && Actions[id].Function.Invoke();

            OnAttemptAction?.Invoke(id);
            if (status)
                OnStartAction?.Invoke(id);

            return status;
        }

        public void InvokeStartAction(string id) => StartAction(id);

        public void SendEvent(string id) => OnEvent?.Invoke(id);

        public void AddAttack(WeaponAttackBase attack)
        {
            AttackModes.Add(attack);
            attack.Parent = this;
        }

        public void RemoveAttack(WeaponAttackBase attack)
        {
            if (AttackModes.Remove(attack))
                attack.Parent = null;
        }

        public void AttackEvent(GameObject[] go)
        {
            OnAttack?.Invoke(go);
            OnWeaponAttack?.Invoke(this);
        }

        public void PreAttackEvent() => OnPreAttack?.Invoke();
        public void SpawnEvent(GameObject go)
        {
            OnSpawn?.Invoke(go);
            OnWeaponSpawn?.Invoke(this, go);
        }

        public void PreSpawnEvent() => OnPreSpawn?.Invoke();

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

        public virtual bool SwitchMode() => SwitchMode(CurrentModeIndex + 1);

        public virtual bool SwitchMode(int index)
        {
            CurrentModeIndex = index;
            return true;
        }

        protected virtual void FixedUpdate() => CurrentMode?.Tick();
    }
}

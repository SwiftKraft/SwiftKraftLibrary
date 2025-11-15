using SwiftKraft.Gameplay.Common.FPS.ViewModels;
using SwiftKraft.Gameplay.Interfaces;
using SwiftKraft.Gameplay.Inventory.Items;
using SwiftKraft.Utils;
using System;
using System.Xml;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Weapons
{
    public class Firearm : EquippedWeaponBase, IAmmo, IAimable
    {
        public const string ReloadAction = "Reload";

        public Shoot AttackState;
        public Idle IdleState = new();
        public Reload ReloadState = new();

        [field: SerializeField]
        public bool Automatic { get; set; }

        [field: SerializeField]
        public int BulletCount { get; set; } = 1;
        [field: SerializeField]
        public int MaxAmmo { get; set; } = 10;
        public Ammo AmmoData = new();

        public int CurrentAmmo
        {
            get => AmmoData != null ? AmmoData.CurrentAmmo : 0; 
            set
            {
                if (AmmoData != null)
                    AmmoData.CurrentAmmo = value;
            }
        }

        public CameraManager CameraManager { get; private set; }
        [Header("Aiming")]
        public float CameraFOV = 70f;
        public float ViewModelFOV = 45f;

        public bool WishAim => Input.GetKey(KeyCode.Mouse1);

        public float AimProgress => AimInterpolater.CurrentValue;

        public SmoothDampInterpolater AimInterpolater;

        CameraManager.FOVOverride.Override mainCamOverride;
        CameraManager.FOVOverride.Override viewModelOverride;

        bool hasCamera;

        protected override void Awake()
        {
            base.Awake();

            AttackStateInstance = AttackState;
            IdleStateInstance = IdleState;

            CameraManager = GetComponentInParent<CameraManager>();

            hasCamera = CameraManager != null;

            if (hasCamera)
            {
                mainCamOverride = CameraManager.MainCameraFOV.AddOverride(CameraFOV);
                viewModelOverride = CameraManager.ViewModelFOV.AddOverride(ViewModelFOV);

                mainCamOverride.Active = false;
                viewModelOverride.Active = false;
            }

            ReloadState.Init(this);

            RegisterAction(ReloadAction, StartReload);
        }

        protected override void Update()
        {
            base.Update();

            bool wishAim = WishAim;

            AimInterpolater.MaxValue = wishAim ? 1f : 0f;
            AimInterpolater.Tick(Time.deltaTime);

            if (hasCamera)
            {
                mainCamOverride.Active = wishAim;
                viewModelOverride.Active = wishAim;
            }
        }

        public override bool Attack() => CurrentAmmo > 0 && CurrentState == IdleStateInstance && base.Attack();

        public virtual bool StartReload()
        {
            if (CurrentAmmo < MaxAmmo && CurrentState == IdleStateInstance)
            {
                CurrentState = ReloadState;
                return true;
            }
            return false;
        }

        public override void Equip(ItemInstance inst)
        {
            base.Equip(inst);
            Instance.TryData("Ammo", out AmmoData, n => n.CurrentAmmo = MaxAmmo);
        }

        public class Ammo : ItemDataBase
        {
            public int CurrentAmmo;
        }

        [Serializable]
        public class Reload : EquippedItemState<Firearm>
        {
            [Serializable]
            public class ReloadProfile
            {
                public float TotalTime = 3f;
                public float FillTime = 1f;
            }

            [Serializable]
            public class ReloadProfileOverride : ReloadProfile
            {
                public int AmmoCount = 30;
            }

            public UnityEvent OnReload;

            public ReloadProfile Default;
            public ReloadProfileOverride[] Overrides;

            public readonly Timer CurrentTimer = new();

            protected BooleanLock.Lock canUnequip;

            private float fillRemain;
            private bool filled;

            public ReloadProfile FindProfile()
            {
                ReloadProfile prof = Default;
                for (int i = 0; i < Overrides.Length; i++)
                {
                    if (Overrides[i].AmmoCount < Item.CurrentAmmo)
                        break;
                    prof = Overrides[i];
                }
                return prof;
            }

            public void SetProfile(ReloadProfile prof)
            {
                CurrentTimer.Reset(prof.TotalTime);
                fillRemain = CurrentTimer.MaxValue - prof.FillTime;
                filled = false;
            }

            public virtual void FillAmmo() => Item.CurrentAmmo = Item.MaxAmmo;

            public override void Init(EquippedItemBase t)
            {
                base.Init(t);
                Array.Sort(Overrides, (a, b) => a.AmmoCount.CompareTo(b.AmmoCount));
                canUnequip = Item.CanUnequip.AddLock();
                canUnequip.Active = false;
            }

            public override void Begin()
            {
                SetProfile(FindProfile());
                OnReload?.Invoke();
                canUnequip.Active = true;
            }

            public override void End()
            {
                if (filled)
                    FillAmmo();
                filled = false;
                canUnequip.Active = false;
            }

            public override void Frame() { }

            public override void Tick()
            {
                CurrentTimer.Tick(Time.fixedDeltaTime);

                if (!filled && CurrentTimer.CurrentValue <= fillRemain)
                    filled = true;

                if (CurrentTimer.Ended)
                    Item.SetIdle();
            }
        }

        [Serializable]
        public class Shoot : BasicAttack
        {
            public new Firearm Item => base.Item as Firearm;

            public override void Begin()
            {
                base.Begin();
                Item.AmmoData.CurrentAmmo--;
            }

            public override bool CheckQueue() => Item.PlayerControlled && Input.GetKeyDown(KeyCode.Mouse0) && Item.CurrentAmmo > 0;
        }

        public class Idle : EquippedItemState<Firearm>
        {
            public override void Begin() { }

            public override void End() { }

            public override void Frame()
            {
                if (Item.PlayerControlled)
                {
                    if (Input.GetKeyDown(KeyCode.Mouse0))
                        Item.PerformAction(AttackAction);

                    if (Input.GetKeyDown(KeyCode.R))
                        Item.PerformAction(ReloadAction);

                    if (Input.GetKeyDown(KeyCode.G))
                        Item.Parent.WishEquip = null;
                }
                else if (Item.CurrentAmmo <= 0)
                    Item.PerformAction(ReloadAction);
            }

            public override void Tick() { }
        }
    }
}

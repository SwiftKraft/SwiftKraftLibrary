using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public abstract class WeaponAds : WeaponComponent
    {
        public const string AimAction = "Aim";

        public bool AdsWhileReload;

        public ModifiableStatistic AimSpeedMultiplier = new(1f);

        public float Aiming { get; protected set; }

        public WeaponAmmo Reloader { get; protected set; }

        public readonly BooleanLock CanAim = new();

        public bool Aim { get; protected set; }

        protected WeaponBase.WeaponAction aimAction;

        protected virtual void Awake()
        {
            Reloader = GetComponent<WeaponAmmo>();
            aimAction = Parent.AddAction(AimAction, () => true);
        }

        protected virtual void FixedUpdate() => Aim = (AdsWhileReload || Reloader == null || !Reloader.Reloading) && CanAim && aimAction.Status;

        protected virtual void OnDestroy()
        {
            Parent.Actions.Remove(AimAction);
            aimAction = null;
        }
    }

    public abstract class WeaponAds<T> : WeaponAds where T : Interpolater, new()
    {
        [SerializeField]
        T aimInterpolater;
        protected virtual void Update()
        {
            aimInterpolater.MaxValue = Aim ? 1f : 0f;
            Aiming = aimInterpolater.Tick(Time.deltaTime * AimSpeedMultiplier);
        }
    }
}

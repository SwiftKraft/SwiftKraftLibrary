using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public abstract class WeaponAds<T> : WeaponComponent where T : Interpolater, new()
    {
        public const string AimAction = "Aim";

        public string AimBlend = "Aim";

        public bool AdsWhileReload;

        public float Aiming { get; private set; }

        public WeaponAmmo Reloader { get; private set; }
        public Animator Animator { get; private set; }

        public readonly BooleanLock CanAim = new();

        [SerializeField]
        T aimInterpolater;
        WeaponBase.WeaponAction aimAction;

        protected virtual void Awake()
        {
            Reloader = GetComponent<WeaponAmmo>();
            Animator = GetComponentInChildren<Animator>();
            aimAction = Parent.AddAction(AimAction, () => true);
        }

        protected virtual void OnDestroy()
        {
            Parent.Actions.Remove(AimAction);
            aimAction = null;
        }

        protected virtual void Update()
        {
            bool aim = (AdsWhileReload || Reloader == null || !Reloader.Reloading) && CanAim && aimAction.Status;

            aimInterpolater.MaxValue = aim ? 1f : 0f;
            Aiming = aimInterpolater.Tick(Time.deltaTime);

            if (Animator != null)
                Animator.SetFloatSafe(AimBlend, Aiming);
        }
    }
}

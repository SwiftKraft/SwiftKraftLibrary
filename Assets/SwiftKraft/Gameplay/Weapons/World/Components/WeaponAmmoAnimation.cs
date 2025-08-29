using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponAmmoAnimation : WeaponAmmo
    {
        public string[] ReloadStates = { "Reload" };
        public string ReloadSpeedParameterName = "ReloadSpeedMultiplier";
        public float FullEndReloadThreshold = 0.9f;

        public Animator Animator
        {
            get
            {
                if (_animator == null)
                    _animator = GetComponentInChildren<Animator>();

                return _animator;
            }
        }
        Animator _animator;

        public override bool Reloading => reloading;
        bool reloading;
        float reloadNormalizedTime;

        protected override void Awake()
        {
            base.Awake();
            _animator = GetComponentInChildren<Animator>();
        }

        protected virtual void Update()
        {
            if (Animator.IsInTransition(0))
                return;

            if (Animator.GetCurrentAnimatorStateInfo(0).CheckName(ReloadStates))
            {
                reloadNormalizedTime = Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                if (reloadNormalizedTime >= 1f)
                    EndReload(true);
            }
            else if (reloading)
            {
                EndReload(reloadNormalizedTime >= FullEndReloadThreshold);
                reloadNormalizedTime = 0f;
            }
        }

        protected override void Reload()
        {
            CanShoot.Active = true;
            OnReloadUpdatedEvent(true);
            reloading = true;
            Animator.SetFloatSafe(ReloadSpeedParameterName, ReloadSpeedMultiplier);
        }

        public override void EndReload(bool fullEnd)
        {
            base.EndReload(fullEnd);
            CanShoot.Active = false;
            OnReloadUpdatedEvent(false);
            reloading = false;
        }
    }
}

using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponAmmoLoopedCommunicator : AnimatorCommunicator<WeaponAmmoLooped>
    {
        public string LoopedAmmoName = "LoopedAmmo";
        public string NextAmmoName = "NextAmmo";

        private void Awake()
        {
            ParentComponent.OnLoadedAmmoChanged += OnLoadedAmmoUpdated;
            ParentComponent.OnNextAmountChanged += OnNextAmountChanged;
        }

        private void Start()
        {
            OnNextAmountChanged(ParentComponent.NextAmount);
            OnLoadedAmmoUpdated(ParentComponent.LoadedAmmo);
        }

        private void OnEnable()
        {
            OnNextAmountChanged(ParentComponent.NextAmount);
            OnLoadedAmmoUpdated(ParentComponent.LoadedAmmo);
        }

        private void OnDestroy()
        {
            ParentComponent.OnLoadedAmmoChanged -= OnLoadedAmmoUpdated;
            ParentComponent.OnNextAmountChanged -= OnNextAmountChanged;
        }

        private void OnNextAmountChanged(int amount) => Animator.SetFloatSafe(NextAmmoName, amount);

        private void OnLoadedAmmoUpdated(int ammo) => Animator.SetFloatSafe(LoopedAmmoName, ammo);
    }
}

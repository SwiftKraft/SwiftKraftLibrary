using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    [RequireComponent(typeof(WeaponAds), typeof(ViewModel))]
    public class WeaponAdsZoomer : RequiredDependencyComponent<WeaponAds>
    {
        public ViewModel ViewModel
        {
            get
            {
                if (_viewModel == null)
                    _viewModel = GetComponent<ViewModel>();

                return _viewModel;
            }
        }
        ViewModel _viewModel;

        public ModifiableStatistic AimViewModelFOV = new(50f);
        public ModifiableStatistic AimCameraFOV = new(75f);

        CameraManager.FOVOverride.Override viewModel;
        CameraManager.FOVOverride.Override mainCam;

        private void Start()
        {
            viewModel = ViewModel.Parent.ViewModelFOV.AddOverride(AimViewModelFOV, 1);
            mainCam = ViewModel.Parent.MainCameraFOV.AddOverride(AimCameraFOV, 1);
        }

        private void OnDisable()
        {
            viewModel.Active = false;
            mainCam.Active = false;
        }

        private void OnDestroy()
        {
            ViewModel.Parent.ViewModelFOV.RemoveOverride(viewModel);
            ViewModel.Parent.MainCameraFOV.RemoveOverride(mainCam);
        }

        private void FixedUpdate()
        {
            if (viewModel.FOV != AimViewModelFOV)
                viewModel.FOV = AimViewModelFOV;
            if (mainCam.FOV != AimCameraFOV)
                mainCam.FOV = AimCameraFOV;

            viewModel.Active = Component.Aim;
            mainCam.Active = Component.Aim;
        }
    }
}

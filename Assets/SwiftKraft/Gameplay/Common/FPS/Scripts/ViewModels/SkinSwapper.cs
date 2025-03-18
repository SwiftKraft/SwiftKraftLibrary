using SwiftKraft.Utils;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class SkinSwapper : MeshSwapper
    {
        public static int CurrentSkin 
        {
            get => _currentSkin;
            set
            {
                _currentSkin = value;
                SkinSwapped?.Invoke(_currentSkin);
            }
        }
        static int _currentSkin;

        protected static event Action<int> SkinSwapped;

        public SkinSwapperPackage SwapperPackage;

        public Package[] Packages => SwapperPackage.Packages;

        protected override void Awake()
        {
            base.Awake();

            if (SwapperPackage == null)
            {
                enabled = false;
                return;
            }

            SwapHand(CurrentSkin);
            SkinSwapped += SwapHand;
        }

        protected virtual void OnDestroy() => SkinSwapped -= SwapHand;

        protected virtual void SwapHand(int index) => SwapMesh(Packages[index % Packages.Length]);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
                Test();
        }

        public void Test() => CurrentSkin = (CurrentSkin + 1) % Packages.Length;
    }
}

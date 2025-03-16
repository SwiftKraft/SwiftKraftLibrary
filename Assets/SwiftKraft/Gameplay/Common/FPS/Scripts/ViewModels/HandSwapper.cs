using SwiftKraft.Utils;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class HandSwapper : MeshSwapper
    {
        public static int CurrentHand 
        {
            get => _currentHand;
            set
            {
                _currentHand = value;
                HandSwapped?.Invoke(_currentHand);
            }
        }
        static int _currentHand;

        protected static event Action<int> HandSwapped;

        public HandSwapperPackage SwapperPackage;

        public Package[] Packages => SwapperPackage.Packages;

        protected override void Awake()
        {
            base.Awake();

            if (SwapperPackage == null)
            {
                enabled = false;
                return;
            }

            SwapHand(CurrentHand);
            HandSwapped += SwapHand;
        }

        protected virtual void OnDestroy() => HandSwapped -= SwapHand;

        protected virtual void SwapHand(int index) => SwapMesh(Packages[index % Packages.Length]);

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
                Test();
        }

        public void Test() => CurrentHand = (CurrentHand + 1) % Packages.Length;
    }
}

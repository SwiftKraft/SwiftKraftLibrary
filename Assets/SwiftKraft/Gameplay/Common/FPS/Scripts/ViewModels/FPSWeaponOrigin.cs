using SwiftKraft.Gameplay.Weapons;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class FPSWeaponOrigin : WeaponOrigin
    {
        public CameraManager CameraManager { get; set; }
        public bool FirstPerson
        {
            get => firstPerson;
            set
            {
                firstPerson = value;
                CurrentOrigin = firstPerson ? FPOrigin : TPOrigin;
            }
        }

        public override Vector3 VisualOrigin { get => FirstPerson ? CameraManager.MainCamera.ScreenToWorldPoint(CameraManager.ViewModelCamera.WorldToScreenPoint(base.VisualOrigin)) : base.VisualOrigin; set => base.VisualOrigin = value; }

        [Header("First-Person")]
        [SerializeField]
        private bool firstPerson;
        public Transform FPOrigin;
        [Header("Third-Person")]
        public Transform TPOrigin;

        protected override void Awake()
        {
            base.Awake();
            CameraManager = GetComponentInParent<CameraManager>();
            FirstPerson = firstPerson;
        }
    }
}

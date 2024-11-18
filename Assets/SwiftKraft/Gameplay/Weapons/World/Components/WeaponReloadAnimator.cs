using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponReloadAnimator : StateMachineBehaviour
    {
        public string[] ReloadStateNames;

        public event Action EndReload;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.CheckName(ReloadStateNames))
                EndReload?.Invoke();
        }
    }
}

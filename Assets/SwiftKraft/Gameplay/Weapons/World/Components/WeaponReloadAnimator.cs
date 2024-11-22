using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponReloadAnimator : StateMachineBehaviour
    {
        public string[] ReloadStateNames;
        public string[] MidReloadStateNames;

        public event Action EndReload;
        public event Action MidReload;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.CheckName(ReloadStateNames))
                EndReload?.Invoke();
        }

        public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.CheckName(MidReloadStateNames) && stateInfo.normalizedTime >= 1f)
                MidReload?.Invoke();
        }
    }
}

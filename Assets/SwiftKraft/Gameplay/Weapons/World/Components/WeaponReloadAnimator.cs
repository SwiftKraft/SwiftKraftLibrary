using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponReloadAnimator : StateMachineBehaviour
    {
        public string[] ReloadStateNames;
        public string[] LoopReloadStateNames;

        public event Action EndReload;
        public event Action MidReload;

        float prevNormalizedTime;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.CheckName(ReloadStateNames))
                EndReload?.Invoke();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.CheckName(LoopReloadStateNames))
            {
                if (stateInfo.normalizedTime % 1f < prevNormalizedTime)
                    MidReload?.Invoke();
                prevNormalizedTime = stateInfo.normalizedTime % 1f;
            }
        }
    }
}

using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Weapons
{
    public class WeaponReloadAnimator : StateMachineBehaviour
    {
        public string[] IdleStateNames = { "Idle" };
        public string[] ReloadStateNames = { "Reload" };
        public string[] MidReloadStateNames;

        public event Action EndReload;
        public event Action MidReload;

        bool midReloaded;

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.CheckName(ReloadStateNames))
                EndReload?.Invoke();
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.CheckName(MidReloadStateNames))
            {
                if (midReloaded && stateInfo.normalizedTime < 1f)
                    midReloaded = false;
                else if (!midReloaded && stateInfo.normalizedTime >= 1f)
                {
                    midReloaded = true;
                    MidReload?.Invoke();
                }
            }
        }
    }
}

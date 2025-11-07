using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public static class AnimatorExtensions
    {
        public static bool HasParameter(this Animator animator, string paramName)
        {
            foreach (AnimatorControllerParameter param in animator.parameters)
                if (param.name == paramName)
                    return true;
            return false;
        }

        public static void SetFloatSafe(this Animator animator, string paramName, float value)
        {
            if (animator.HasParameter(paramName))
                animator.SetFloat(paramName, value);
        }

        public static void SetIntegerSafe(this Animator animator, string paramName, int value)
        {
            if (animator.HasParameter(paramName))
                animator.SetInteger(paramName, value);
        }

        public static void SetBoolSafe(this Animator animator, string paramName, bool value)
        {
            if (animator.HasParameter(paramName))
                animator.SetBool(paramName, value);
        }

        public static bool CheckName(this AnimatorStateInfo info, ICollection<string> stateNames)
        {
            foreach (string str in stateNames)
                if (info.IsName(str))
                    return true;
            return false;
        }
    }
}

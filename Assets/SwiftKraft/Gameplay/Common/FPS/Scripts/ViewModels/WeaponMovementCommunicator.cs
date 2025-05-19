using SwiftKraft.Gameplay.Weapons;
using SwiftKraft.Utils;
using System;
using UnityEngine;

namespace SwiftKraft.Gameplay.Common.FPS.ViewModels
{
    public class WeaponMovementCommunicator : AnimatorCommunicator<WeaponMovement>
    {
        public string MultiplierName = "MovementSpeedMultiplier";
        public string ParameterName = "Movement";

        public bool Raw = false;
        public float Multiplier = 1f;
        public StateItem[] StateMultipliers;

        private void Update()
        {
            Animator.SetFloatSafe(ParameterName, ParentComponent.State);
            Animator.SetFloatSafe(MultiplierName, (Raw ? ParentComponent.Motor.RawMoveFactorRate : ParentComponent.Motor.MoveFactorRate) * GetMultiplier() * Multiplier);
        }

        public float GetMultiplier()
        {
            if (StateMultipliers.Length <= 0)
                return 1f;

            foreach (StateItem st in StateMultipliers)
                if (st.State == ParentComponent.Motor.State)
                    return st.Multiplier;

            return 1f;
        }

        [Serializable]
        public struct StateItem
        {
            public int State;
            public float Multiplier;
        }
    }
}

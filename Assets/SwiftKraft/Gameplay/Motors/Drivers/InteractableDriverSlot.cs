using SwiftKraft.Gameplay.Interactions;
using SwiftKraft.Gameplay.Interfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Gameplay.Motors
{
    public class InteractableDriverSlot : MonoBehaviour, IInteractable
    {
        public MotorDriver Parent { get; private set; }

        public int Index;

        private void Awake()
        {
            Parent = GetComponentInParent<MotorDriver>();
        }

        public void Interact(InteractorBase interactor)
        {
            if (interactor.TryGetComponent(out MotorBase motor)
                && !Parent.SetDriverSafe(motor, Index)
                && Parent.TryGetDriver(Index, out MotorDriver.DriverSlot slot)
                && slot.Reference == motor)
                    Parent.SetDriver(null, Index);
        }
    }
}

using SwiftKraft.Gameplay.Interactions;
using SwiftKraft.Gameplay.Interfaces;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Map
{
    public abstract class DoorBase : MonoBehaviour, IInteractable
    {
        public bool IsOpen
        {
            get => _isOpen;
            protected set
            {
                if (_isOpen == value)
                    return;

                OnOpenChanged?.Invoke(value);
                _isOpen = value;
            }
        }
        bool _isOpen;

        public float Status
        {
            get => _status; 
            protected set
            {
                if (_status == value)
                    return;

                OnStatusChanged?.Invoke(value);
                _status = value;
            }
        }
        float _status;

        public UnityEvent<bool> OnOpenChanged;
        public UnityEvent<float> OnStatusChanged;
        public UnityEvent<InteractorBase> OnInteract;

        InteractorBase prevInteractor;

        public virtual void SetState(bool isOpen)
        {
            IsOpen = isOpen;
            Status = isOpen ? 1f : 0f;
        }

        public virtual void Interact(InteractorBase interactor)
        {
            if (prevInteractor == interactor)
                return;

            SetState(!IsOpen);
            prevInteractor = interactor;
            OnInteract?.Invoke(interactor);
            prevInteractor = null;
        }
    }
}

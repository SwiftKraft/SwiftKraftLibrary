using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace SwiftKraft.Gameplay.Interactions
{
    public class InteractableUnityEvent : MonoBehaviour, IInteractable
    {
        public UnityEvent<InteractorBase> OnInteract;

        public void Interact(InteractorBase interactor) => OnInteract?.Invoke(interactor);
    }
}

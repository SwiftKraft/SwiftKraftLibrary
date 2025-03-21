using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Interactions
{
    public abstract class InteractorBase : MonoBehaviour
    {
        public abstract IInteractable Detect();

        public virtual void Interact(IInteractable interactable)
        {
            if ((Object)interactable == null)
                return;

            interactable.Interact(this);
        }
    }
}

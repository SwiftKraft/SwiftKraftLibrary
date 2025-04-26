using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Interactions
{
    public class InteractionExtender : MonoBehaviour, IInteractable
    {
        public IInteractable Parent { get; private set; }

        private void Awake()
        {
            Parent = transform.parent.GetComponentInParent<IInteractable>();

            if ((Object)Parent == this)
                Parent = null;
        }

        public void Interact(InteractorBase interactor)
        {
            if ((Object)Parent != null)
                Parent.Interact(interactor);
        }
    }
}

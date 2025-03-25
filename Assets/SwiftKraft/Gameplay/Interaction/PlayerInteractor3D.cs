using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;

namespace SwiftKraft.Gameplay.Interactions
{
    public class PlayerInteractor3D : InteractorBase
    {
        public LayerMask InteractionLayers;
        public QueryTriggerInteraction TriggerInteraction;
        public float InteractionDistance = 2f;
        public Transform TargetTransform;
        public KeyCode Key;

        protected virtual void Update()
        {
            if (Input.GetKeyDown(Key))
                Interact(Detect());
        }

        public override IInteractable Detect() => Physics.Raycast(TargetTransform.position, TargetTransform.forward,
                out RaycastHit hitInfo, InteractionDistance, InteractionLayers, TriggerInteraction)
                && hitInfo.transform.TryGetComponent(out IInteractable interactable)
                ? interactable
                : null;
    }
}

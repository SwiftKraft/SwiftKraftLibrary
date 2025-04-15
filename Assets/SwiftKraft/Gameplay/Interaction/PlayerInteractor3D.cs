using SwiftKraft.Gameplay.Interfaces;
using UnityEngine;
using static SwiftKraft.Gameplay.Projectiles.ProjectileBase;

namespace SwiftKraft.Gameplay.Interactions
{
    public class PlayerInteractor3D : InteractorBase
    {
        public LayerMask InteractionLayers;
        public LayerMask LOSLayers;
        public QueryTriggerInteraction TriggerInteraction;
        public float InteractionDistance = 2f;
        public float InteractionRadius = 0.5f;
        public Transform TargetTransform;
        public KeyCode Key;

        protected virtual void Update()
        {
            if (Input.GetKeyDown(Key))
                Interact(Detect());
        }

        public override IInteractable Detect()
        {
            RaycastHit[] hits = Physics.SphereCastAll(TargetTransform.position, InteractionRadius, TargetTransform.forward,
                InteractionDistance, InteractionLayers, TriggerInteraction);


            IInteractable selected = null;
            Vector3 selectedPoint = default;
            foreach (RaycastHit hitInfo in hits)
            {
                if (hitInfo.transform.TryGetComponent(out IInteractable interactable)
                    && HasLOS(hitInfo)
                    && (selected == null || (selectedPoint - TargetTransform.position).sqrMagnitude > (hitInfo.point - TargetTransform.position).sqrMagnitude))
                {
                    selected = interactable;
                    selectedPoint = hitInfo.point;
                }
            }

            return selected;
        }

        public bool HasLOS(RaycastHit hitInfo)
        {
            Collider collider = hitInfo.collider;
            collider.enabled = false;
            bool los = !Physics.Linecast(TargetTransform.position, hitInfo.point, LOSLayers, QueryTriggerInteraction.Ignore);
            collider.enabled = true;
            return los;
        }
    }
}

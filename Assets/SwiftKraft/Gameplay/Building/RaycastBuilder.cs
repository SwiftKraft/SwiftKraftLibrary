using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class RaycastBuilder : MonoBehaviour
    {
        public Material BlueprintMaterial;

        public Transform CastPoint;

        public float CastRange = 5f;
        public LayerMask CastLayers;

        public GameObject test;

        public GameObject Prefab
        {
            get => _prefab;
            private set
            {
                RemoveBlueprint();
                _prefab = value;
                UpdateBlueprint();
                CreateBlueprint(aimedPoint, currentRotation);
            }
        }
        GameObject _prefab;

        GameObject currentBlueprint;

        Vector3 aimedPoint;
        Quaternion currentRotation;

        private void Start()
        {
            Prefab = test;
        }

        private void Update()
        {
            if (currentBlueprint == null)
                return;

            UpdateBlueprint();
        }

        public void UpdateBlueprint() =>
            aimedPoint = Physics.Raycast(CastPoint.position, CastPoint.forward, out RaycastHit _hit, CastRange, CastLayers, QueryTriggerInteraction.Ignore)
                ? _hit.point
                : CastPoint.position + CastPoint.forward * CastRange;

        public void CreateBlueprint(Vector3 position, Quaternion rotation)
        {
            if (Prefab == null)
                return;

            GameObject go = BuildingManager.ToBlueprint(Prefab, BlueprintMaterial);
            go.transform.SetPositionAndRotation(position, rotation);
            currentBlueprint = go;
        }

        public void RemoveBlueprint()
        {
            if (currentBlueprint != null)
                Destroy(currentBlueprint);
        }
    }
}

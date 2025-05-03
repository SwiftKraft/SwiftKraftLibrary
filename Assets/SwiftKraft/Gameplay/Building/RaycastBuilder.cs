using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class RaycastBuilder : MonoBehaviour
    {
        public Material ValidMaterial;
        public Material InvalidMaterial;

        public Transform CastPoint;

        public float CastRange = 5f;
        public LayerMask CastLayers;

        public GameObject test;

        public bool UseNormal;

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

        Blueprint currentBlueprint;

        Vector3 aimedPoint;
        Quaternion currentRotation = Quaternion.identity;
        bool canBuild;

        private void Start()
        {
            Prefab = test;
        }

        private void Update()
        {
            UpdateBlueprint();

            if (Input.GetKeyDown(KeyCode.Mouse0) && canBuild)
                Build();
        }

        public void Build()
        {
            if (currentBlueprint == null || Prefab == null)
                return;

            Instantiate(Prefab, currentBlueprint.transform.position, currentBlueprint.transform.rotation);
        }

        public void UpdateBlueprint()
        {
            if (currentBlueprint == null)
                return;

            bool raycast = Physics.Raycast(CastPoint.position, CastPoint.forward, out RaycastHit _hit, CastRange, CastLayers, QueryTriggerInteraction.Ignore);

            aimedPoint = raycast
                ? _hit.point
                : CastPoint.position + CastPoint.forward * CastRange;

            bool nextCanBuild = raycast;

            if (canBuild != nextCanBuild)
            {
                canBuild = nextCanBuild;
                currentBlueprint.ChangeMaterial(canBuild ? ValidMaterial : InvalidMaterial);
            }

            currentBlueprint.transform.SetPositionAndRotation(aimedPoint, currentRotation * (UseNormal && raycast ? Quaternion.FromToRotation(currentRotation * Vector3.up, _hit.normal) : Quaternion.identity));
        }

        public void CreateBlueprint(Vector3 position, Quaternion rotation)
        {
            if (Prefab == null)
                return;

            Blueprint go = BuildingManager.ToBlueprint(Prefab);
            go.transform.SetPositionAndRotation(position, rotation);
            currentBlueprint = go;
            go.ChangeMaterial(canBuild ? ValidMaterial : InvalidMaterial);
        }

        public void RemoveBlueprint()
        {
            if (currentBlueprint != null)
                Destroy(currentBlueprint);
        }
    }
}

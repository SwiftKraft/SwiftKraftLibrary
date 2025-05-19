using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class RaycastBuilder : MonoBehaviour
    {
        public Material ValidMaterial;
        public Material InvalidMaterial;

        public Transform CastPoint;

        public Vector3 SnapGrid;
        public Vector3 SnapGridOffset;

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

            if (currentBlueprint == null)
                return;

            if (Input.GetKeyDown(KeyCode.Mouse0) && canBuild)
                Build();
            if (Input.GetKeyDown(KeyCode.Mouse1))
                RemoveBlueprint();
        }

        private void OnDestroy() => RemoveBlueprint();

        public void Build()
        {
            if (currentBlueprint == null || Prefab == null || BuildingManager.Instance == null)
                return;

            BuildingManager.Instance.Create(Prefab, new(currentBlueprint.transform));
        }

        public void UpdateBlueprint()
        {
            if (currentBlueprint == null)
                return;

            bool raycast = Physics.Raycast(CastPoint.position, CastPoint.forward, out RaycastHit _hit, CastRange, CastLayers, QueryTriggerInteraction.Ignore);
            bool hasSnapPoint = false;
            Vector3 point = _hit.point;
            Quaternion rotation = Quaternion.identity;

            if (raycast)
            {
                if (_hit.transform.TryGetComponent(out IBuildingSnapPoint snapPoint))
                {
                    point = snapPoint.SnapPosition;
                    rotation = snapPoint.SnapRotation;
                    hasSnapPoint = true;
                }
                else if (SnapGrid != default)
                {
                    raycast = Physics.Raycast(CastPoint.position, _hit.point.GridSnap(SnapGrid, SnapGridOffset) - CastPoint.position, out _hit, CastRange, CastLayers, QueryTriggerInteraction.Ignore);
                    point = _hit.point;
                }
            }

            aimedPoint = raycast
                ? point
                : CastPoint.position + CastPoint.forward * CastRange;

            bool nextCanBuild = raycast;

            if (canBuild != nextCanBuild)
            {
                canBuild = nextCanBuild;
                currentBlueprint.ChangeMaterial(canBuild ? ValidMaterial : InvalidMaterial);
            }

            currentBlueprint.transform.SetPositionAndRotation(aimedPoint, currentRotation * rotation * (UseNormal && raycast && !hasSnapPoint ? Quaternion.FromToRotation(currentRotation * Vector3.up, _hit.normal) : Quaternion.identity));
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
                Destroy(currentBlueprint.gameObject);
        }
    }
}

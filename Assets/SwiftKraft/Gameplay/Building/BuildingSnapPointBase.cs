using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public class BuildingSnapPointBase : MonoBehaviour, IBuildingSnapPoint
    {
        public Transform Point;

        public Vector3 SnapPosition => Point != null ? Point.position : transform.position;
        public Quaternion SnapRotation => Point != null ? Point.rotation : transform.rotation;
    }
}

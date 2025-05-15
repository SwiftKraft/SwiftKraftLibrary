using UnityEngine;

namespace SwiftKraft.Gameplay.Building
{
    public interface IBuildingSnapPoint
    {
        public Vector3 SnapPosition { get; }
        public Quaternion SnapRotation { get; }
    }
}

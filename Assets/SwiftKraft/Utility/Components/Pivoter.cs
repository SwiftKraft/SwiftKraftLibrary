using UnityEngine;

namespace SwiftKraft.Utils
{
    public class Pivoter : OptionalModifyTransformComponent
    {
        public Transform RelativeTarget;

        public Vector3 Offset;

        private void Update() => Position = -RelativeTarget.localPosition + Offset;
    }
}

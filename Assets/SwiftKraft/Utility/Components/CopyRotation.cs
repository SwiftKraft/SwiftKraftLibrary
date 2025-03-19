using UnityEngine;

namespace SwiftKraft.Utils
{
    public class CopyRotation : MonoBehaviour
    {
        public Transform ReferenceTransform;
        public Transform TargetTransform;

        public Vector3 Offset;

        public Vector4 AxisMultiplier = Vector4.one;
        public float Multiplier = 1f;

        [field: SerializeField]
        public bool Local { get; set; }

        public Quaternion OriginalRotation { get; set; }

        private void Awake()
        {
            if (!Local)
                OriginalRotation = transform.rotation;
            else
                OriginalRotation = transform.localRotation;
        }

        private void Update() => UpdateRotation();

        private void FixedUpdate() => UpdateRotation();

        public void UpdateRotation() => SetRotation(TargetTransform == null ?
            OriginalRotation :
            (Local ?
            (ReferenceTransform != null ?
            Quaternion.Inverse(ReferenceTransform.rotation) * TargetTransform.rotation :
            TargetTransform.localRotation) : 
            TargetTransform.rotation));

        public void SetRotation(Quaternion rot)
        {
            rot *= Quaternion.Euler(Offset);
            rot.w *= AxisMultiplier.w;
            rot.x *= AxisMultiplier.x;
            rot.y *= AxisMultiplier.y;
            rot.z *= AxisMultiplier.z;
            Quaternion quat = Quaternion.SlerpUnclamped(OriginalRotation, rot, Multiplier);

            if (!Local)
                transform.rotation = quat;
            else
                transform.localRotation = quat;
        }
    }
}

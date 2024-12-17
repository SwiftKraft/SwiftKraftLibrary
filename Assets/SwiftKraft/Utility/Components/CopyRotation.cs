using UnityEngine;

namespace SwiftKraft.Utils
{
    public class CopyRotation : MonoBehaviour
    {
        public Transform ReferenceTransform;
        public Transform TargetTransform;

        public Vector3 Offset;

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
            TargetTransform.localRotation : 
            (ReferenceTransform != null ? 
            Quaternion.Inverse(ReferenceTransform.rotation) : 
            Quaternion.identity) *
            TargetTransform.rotation));

        public void SetRotation(Quaternion rot)
        {
            Quaternion quat = Quaternion.SlerpUnclamped(OriginalRotation, rot * Quaternion.Euler(Offset), Multiplier);

            if (!Local)
                transform.rotation = quat;
            else
                transform.localRotation = quat;
        }
    }
}

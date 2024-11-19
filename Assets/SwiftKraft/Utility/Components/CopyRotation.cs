using UnityEngine;

namespace SwiftKraft.Utils
{
    public class CopyRotation : MonoBehaviour
    {
        public Transform TargetTransform;

        [field: SerializeField]
        public bool Local { get; private set; }

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
    (Local ? TargetTransform.localRotation : TargetTransform.rotation));

        public void SetRotation(Quaternion rot)
        {
            if (!Local)
                transform.rotation = rot;
            else
                transform.localRotation = rot;
        }
    }
}

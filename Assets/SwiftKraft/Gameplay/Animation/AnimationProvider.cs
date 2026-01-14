using SwiftKraft.Utils;
using UnityEngine;

namespace SwiftKraft.Gameplay.Animation
{
    public class AnimationProvider : MonoBehaviour
    {
        [field: SerializeField]
        public AnimationReceiver Target { get; set; }
        [field: SerializeField]
        public Transform Root { get; private set; }
        [field: SerializeField]
        public int RootIndex { get; set; } = 0;

        [field: SerializeField]
        public bool Active { get; set; } = true;

        Vector3 posOffset;

        private void Start()
        {
            Target = GetComponentInParent<AnimationReceiver>();
            posOffset = transform.position - Target.Roots[RootIndex].position;
        }

        private void LateUpdate()
        {
            if (!Active || Target == null || Root == null)
                return;

            if (Target.Roots.InRange(RootIndex))
            {
                Target.Copy(Root, RootIndex);
                Transform tr = Target.Roots[RootIndex];
                transform.position = tr.position + posOffset;
            }
        }
    }
}

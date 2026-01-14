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

        private void Start()
        {
            if (Target == null)
                Target = GetComponentInParent<AnimationReceiver>();
        }

        private void LateUpdate()
        {
            if (!Active || Target == null || Root == null)
                return;

            Target.Copy(Root, RootIndex);
        }
    }
}
